using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Content.ScriptableObjects;
using Content.ScriptableObjects.Editor;
using Sapientia;
using Sapientia.Extensions;
using Sapientia.Pooling;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	using UnityObject = UnityEngine.Object;

	public static class ContentValidator
	{
		private const string TITLE = "Validate Content";

		private const string SYNC_BEFORE_VALIDATE_PREF_KEY = "ContentValidator.SyncBeforeValidate";
		private const string SYNC_BEFORE_VALIDATE_MENU = ContentMenuConstants.VALIDATION_MENU + "Sync Before Validate";

		private static bool SyncBeforeValidate { get => EditorPrefs.GetBool(SYNC_BEFORE_VALIDATE_PREF_KEY, true); set => EditorPrefs.SetBool(SYNC_BEFORE_VALIDATE_PREF_KEY, value); }

		private static StringBuilder _activeErrorMessageBuilder;
		private static int _activeErrorMessageNumber;
		private static IContentValueValidator _activeAdditionalValidator;
		private static bool _cancelRequested;

		static ContentValidator()
		{
			AssemblyReloadEvents.beforeAssemblyReload += CancelBeforeAssemblyReload;
		}

		private static void CancelBeforeAssemblyReload()
		{
			if (Application.isBatchMode)
				return;

			_cancelRequested = true;
		}

		[MenuItem(SYNC_BEFORE_VALIDATE_MENU, priority = 99)]
		public static void ToggleSyncBeforeValidate()
		{
			SyncBeforeValidate = !SyncBeforeValidate;
			Menu.SetChecked(SYNC_BEFORE_VALIDATE_MENU, SyncBeforeValidate);
		}

		[MenuItem(SYNC_BEFORE_VALIDATE_MENU, true, priority = 99)]
		public static bool ToggleSyncBeforeValidateValidate()
		{
			Menu.SetChecked(SYNC_BEFORE_VALIDATE_MENU, SyncBeforeValidate);
			return true;
		}

		[MenuItem(ContentMenuConstants.VALIDATION_MENU + "Validate", priority = 119)]
		private static void ValidateRunEditor()
		{
			Validate();
		}

		public static bool Validate()
		{
			return Validate(SyncBeforeValidate, out _);
		}

		public static bool TryGetValidator<T>(out T validator)
			where T : class, IContentValueValidator
		{
			validator = ContentValidationSettings.Settings.GetEnabledCustomValidator<T>();
			return validator != null;
		}

		public static bool Validate(bool sync, out string errorOrMessage)
		{
			_cancelRequested = false;

			if (sync)
				ContentDatabaseEditorUtility.SyncContent();

			if (IsValidationCancellationRequested())
			{
				errorOrMessage = null;
				return CancelValidation();
			}

			var errorCount = 0;
			var warningCount = 0;

			var dbs = ContentEditorCache.GetAssets<ContentDatabaseScriptableObject>()
				.ToList();
			var valueValidators = GetEnabledValidators();

			float totalProgress = 0;
			foreach (var database in dbs)
				totalProgress += database.scriptableObjects?.Count ?? 0;

			var progress = 0;
			try
			{
				using (StringBuilderPool.Get(out var errStringBuilder))
				{
					var previousErrorMessageBuilder = _activeErrorMessageBuilder;
					var previousErrorMessageNumber = _activeErrorMessageNumber;
					_activeErrorMessageBuilder = errStringBuilder;
					_activeErrorMessageNumber = 0;

					try
					{
						foreach (var database in dbs)
						{
							if (database.scriptableObjects == null)
							{
								var errorMessage = $"Null scriptable objects list in database [ {database.name} ]";
								errorCount++;
								AppendErrorMessage(errStringBuilder, errorMessage);
								ContentDebug.LogError(errorMessage, database);
								continue;
							}

							foreach (var scriptableObject in database.scriptableObjects)
							{
								if (!Application.isBatchMode)
								{
									var scriptableObjectName = scriptableObject ? scriptableObject.name : "null";
									var label = $"{database.name} / {scriptableObjectName}";
									var progressValue = totalProgress > 0 ? progress / totalProgress : 1;
									if (EditorUtility.DisplayCancelableProgressBar(TITLE,
										label,
										progressValue))
									{
										errorOrMessage = null;
										return CancelValidation("user");
									}
								}

								if (IsValidationCancellationRequested())
								{
									errorOrMessage = null;
									return CancelValidation();
								}

								progress++;

								if (!scriptableObject)
								{
									var errorMessage = $"Null scriptable object in database [ {database.name} ]";
									errorCount++;
									AppendErrorMessage(errStringBuilder, errorMessage);
									ContentDebug.LogError(errorMessage, database);
									continue;
								}

								if (scriptableObject.SkipValidation())
									continue;

								if (scriptableObject is IValidatable validatable)
								{
									if (!validatable.Validate(out var soMessage))
									{
										errorCount++;
										AppendErrorMessage(errStringBuilder, soMessage);
										ContentDebug.LogError(soMessage, scriptableObject);
									}
								}

								errorCount += ValidateContentReferences(scriptableObject, valueValidators, ref warningCount, errStringBuilder);
							}
						}

						if (errorCount > 0)
						{
							var str = $"failed (errors: {errorCount}, warnings️: {warningCount})"
								+ (errStringBuilder.Length > 0
									? ", errors:\n" + errStringBuilder
									: string.Empty);
							ContentDebug.LogError("Validation " + str);
							errorOrMessage = "Content validation " + str;
							return false;
						}

						errorOrMessage = warningCount > 0
							? $"Validation passed (warnings️: {warningCount})"
							: "Validation passed";
						ContentDebug.Log(errorOrMessage);
					}
					finally
					{
						_activeErrorMessageBuilder = previousErrorMessageBuilder;
						_activeErrorMessageNumber = previousErrorMessageNumber;
					}
				}
			}
			finally
			{
				if (!Application.isBatchMode)
					EditorUtility.ClearProgressBar();
			}

			return true;
		}

		public static IReadOnlyList<IContentValueValidator> GetEnabledValidators()
		{
			return AddValidator(
				ContentValidationSettings.Settings.GetEnabledCustomValidators(),
				_activeAdditionalValidator);
		}

		public static int ValidateContentObject(object target,
			Type targetType,
			string path,
			ContentScriptableObject source,
			object logContext,
			IReadOnlyList<IContentValueValidator> valueValidators,
			ref int warningCount,
			bool inspectUnityObject = false,
			Func<string, string> logMessageFormatter = null,
			StringBuilder errorMessageBuilder = null,
			IContentValueValidator additionalValidator = null)
		{
			errorMessageBuilder ??= _activeErrorMessageBuilder;

			var previousAdditionalValidator = _activeAdditionalValidator;
			if (additionalValidator != null)
				_activeAdditionalValidator = additionalValidator;

			try
			{
				var validators = AddValidator(valueValidators, additionalValidator);
				var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
				return ValidateContentReferences(target,
					targetType,
					path,
					source,
					logContext ?? source,
					visited,
					false,
					false,
					validators,
					ref warningCount,
					inspectUnityObject,
					logMessageFormatter,
					errorMessageBuilder);
			}
			finally
			{
				if (additionalValidator != null)
					_activeAdditionalValidator = previousAdditionalValidator;
			}
		}

		private static IReadOnlyList<IContentValueValidator> AddValidator(
			IReadOnlyList<IContentValueValidator> validators,
			IContentValueValidator additionalValidator)
		{
			if (additionalValidator == null)
				return validators;

			var count = validators?.Count ?? 0;
			for (var i = 0; i < count; i++)
			{
				if (ReferenceEquals(validators[i], additionalValidator))
					return validators;
			}

			var result = new IContentValueValidator[count + 1];
			for (var i = 0; i < count; i++)
				result[i] = validators[i];
			result[count] = additionalValidator;
			return result;
		}

		private static bool IsValidationCancellationRequested()
		{
			return !Application.isBatchMode && (_cancelRequested || EditorApplication.isCompiling);
		}

		private static bool CancelValidation(string reason = null)
		{
			reason ??= _cancelRequested
				? "domain reload"
				: "editor compilation";
			ContentDebug.LogWarning($"Validation canceled ({reason})");
			return false;
		}

		private static int ValidateContentReferences(ContentScriptableObject scriptableObject,
			IReadOnlyList<IContentValueValidator> valueValidators,
			ref int warningCount,
			StringBuilder errorMessageBuilder)
		{
			if (!scriptableObject)
				return 0;

			var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
			return ValidateContentReferences(scriptableObject,
				scriptableObject.GetType(),
				scriptableObject.name,
				scriptableObject,
				scriptableObject,
				visited,
				false,
				false,
				valueValidators,
				ref warningCount,
				false,
				null,
				errorMessageBuilder);
		}

		private static int ValidateContentReferences(object target,
			Type targetType,
			string path,
			ContentScriptableObject context,
			object logContext,
			HashSet<object> visited,
			bool canBeEmpty,
			bool failIfEmpty,
			IReadOnlyList<IContentValueValidator> valueValidators,
			ref int warningCount,
			bool inspectUnityObject,
			Func<string, string> logMessageFormatter,
			StringBuilder errorMessageBuilder)
		{
			if (targetType == null)
				return 0;

			var errorCount = ValidateContentValue(target,
				targetType,
				path,
				context,
				logContext,
				canBeEmpty,
				valueValidators,
				logMessageFormatter,
				errorMessageBuilder);
			if (target == null)
				return errorCount;

			if (target is IContentReference reference)
				return errorCount + ValidateContentReference(reference,
					path,
					logContext,
					canBeEmpty,
					failIfEmpty,
					ref warningCount,
					logMessageFormatter,
					errorMessageBuilder);

			if (IsTerminal(targetType))
				return errorCount;

			if (target is UnityObject && !inspectUnityObject && target != context)
				return errorCount;

			if (targetType.IsClass && !visited.Add(target))
				return errorCount;

			if (target is IDictionary dictionary)
				return errorCount + ValidateDictionary(dictionary,
					path,
					context,
					logContext,
					visited,
					canBeEmpty,
					failIfEmpty,
					valueValidators,
					ref warningCount,
					logMessageFormatter,
					errorMessageBuilder);

			if (target is IEnumerable enumerable && target is not string)
				return errorCount + ValidateEnumerable(enumerable,
					path,
					context,
					logContext,
					visited,
					canBeEmpty,
					failIfEmpty,
					valueValidators,
					ref warningCount,
					logMessageFormatter,
					errorMessageBuilder);

			foreach (var field in GetSerializableFields(targetType))
			{
				if (field.Name == nameof(ContentDatabaseScriptableObject.scriptableObjects))
					continue;

				object value;
				try
				{
					value = field.GetValue(target);
				}
				catch (Exception e)
				{
					var errorMessage = FormatLogMessage($"Content validation: can't read field [ {path}.{field.Name} ]: {e.Message}",
						logMessageFormatter);
					AppendErrorMessage(errorMessageBuilder, errorMessage);
					ContentDebug.LogError(errorMessage, logContext);
					errorCount++;
					continue;
				}

				errorCount += ValidateContentReferences(value,
					value?.GetType() ?? field.FieldType,
					$"{path}.{field.Name}",
					context,
					logContext,
					visited,
					CanBeEmpty(field),
					FailIfEmpty(field),
					valueValidators,
					ref warningCount,
					false,
					logMessageFormatter,
					errorMessageBuilder);
			}

			return errorCount;
		}

		private static int ValidateContentValue(object value,
			Type valueType,
			string path,
			ContentScriptableObject context,
			object logContext,
			bool canBeEmpty,
			IReadOnlyList<IContentValueValidator> validators,
			Func<string, string> logMessageFormatter,
			StringBuilder errorMessageBuilder)
		{
			if (validators == null || validators.Count == 0)
				return 0;

			var errorCount = 0;
			var validationContext = new ContentValidationContext(value, valueType, path, context, canBeEmpty);
			foreach (var validator in validators)
			{
				if (validator == null)
					continue;

				string message;
				try
				{
					if (validator.Validate(validationContext, out message))
						continue;
				}
				catch (Exception e)
				{
					var errorMessage = FormatLogMessage(
						$"Content value validator [ {validator.GetType().Name} ] failed at [ {path} ]: {e.Message}",
						logMessageFormatter);
					AppendErrorMessage(errorMessageBuilder, errorMessage);
					ContentDebug.LogError(errorMessage, logContext);
					errorCount++;
					continue;
				}

				if (message == null)
				{
					errorCount++;
					continue;
				}

				if (message.IsNullOrEmpty())
					message = $"Invalid content value [ {path} ] by type [ {valueType.Name} ] and validator [ {validator.GetType().Name} ]";

				var formattedMessage = FormatLogMessage(message, logMessageFormatter);
				AppendErrorMessage(errorMessageBuilder, formattedMessage);
				ContentDebug.LogError(formattedMessage, logContext);
				errorCount++;
			}

			return errorCount;
		}

		private static int ValidateDictionary(IDictionary dictionary,
			string path,
			ContentScriptableObject context,
			object logContext,
			HashSet<object> visited,
			bool canBeEmpty,
			bool failIfEmpty,
			IReadOnlyList<IContentValueValidator> valueValidators,
			ref int warningCount,
			Func<string, string> logMessageFormatter,
			StringBuilder errorMessageBuilder)
		{
			var errorCount = 0;
			foreach (DictionaryEntry entry in dictionary)
			{
				errorCount += ValidateContentReferences(entry.Key,
					entry.Key?.GetType(),
					$"{path}[key: {entry.Key}]",
					context,
					logContext,
					visited,
					canBeEmpty,
					failIfEmpty,
					valueValidators,
					ref warningCount,
					false,
					logMessageFormatter,
					errorMessageBuilder);

				errorCount += ValidateContentReferences(entry.Value,
					entry.Value?.GetType(),
					$"{path}[{entry.Key}]",
					context,
					logContext,
					visited,
					canBeEmpty,
					failIfEmpty,
					valueValidators,
					ref warningCount,
					false,
					logMessageFormatter,
					errorMessageBuilder);
			}

			return errorCount;
		}

		private static int ValidateEnumerable(IEnumerable enumerable,
			string path,
			ContentScriptableObject context,
			object logContext,
			HashSet<object> visited,
			bool canBeEmpty,
			bool failIfEmpty,
			IReadOnlyList<IContentValueValidator> valueValidators,
			ref int warningCount,
			Func<string, string> logMessageFormatter,
			StringBuilder errorMessageBuilder)
		{
			var errorCount = 0;
			var index = 0;
			foreach (var item in enumerable)
			{
				errorCount += ValidateContentReferences(item,
					item?.GetType(),
					$"{path}[{index}]",
					context,
					logContext,
					visited,
					canBeEmpty,
					failIfEmpty,
					valueValidators,
					ref warningCount,
					false,
					logMessageFormatter,
					errorMessageBuilder);
				index++;
			}

			return errorCount;
		}

		private static int ValidateContentReference(IContentReference reference,
			string path,
			object logContext,
			bool canBeEmpty,
			bool failIfEmpty,
			ref int warningCount,
			Func<string, string> logMessageFormatter,
			StringBuilder errorMessageBuilder)
		{
			if (reference.IsEmpty())
			{
				if (failIfEmpty)
				{
					var errorMessage = FormatLogMessage($"Empty content reference [ {path} ] with [NotEmpty] or [NotNull] attribute",
						logMessageFormatter);
					AppendErrorMessage(errorMessageBuilder, errorMessage);
					ContentDebug.LogError(errorMessage, logContext);
					return 1;
				}

				if (!canBeEmpty)
				{
					warningCount++;
					ContentDebug.LogWarning(
						FormatLogMessage($"Empty content reference [ {path} ] without [CanBeEmpty], [CanBeNull] or [MaybeNull] attribute",
							logMessageFormatter),
						logContext);
				}

				return 0;
			}

			Type valueType;
			try
			{
				valueType = reference.ValueType;
			}
			catch (Exception)
			{
				return 0;
			}

			if (reference.IsValid())
				return 0;

			var invalidReferenceMessage = FormatLogMessage($"Invalid content reference [ {path} ] by type [ {valueType.Name} ] and guid [ {reference.Guid} ]",
				logMessageFormatter);
			AppendErrorMessage(errorMessageBuilder, invalidReferenceMessage);
			ContentDebug.LogError(invalidReferenceMessage, logContext);
			return 1;
		}

		private static void AppendErrorMessage(StringBuilder stringBuilder, string message)
		{
			if (stringBuilder == null || message.IsNullOrEmpty())
				return;

			var number = stringBuilder == _activeErrorMessageBuilder
				? ++_activeErrorMessageNumber
				: GetNextErrorMessageNumber(stringBuilder);

			stringBuilder.Append("[");
			stringBuilder.Append(number);
			stringBuilder.Append("] ");
			stringBuilder.AppendLine(message);
		}

		private static int GetNextErrorMessageNumber(StringBuilder stringBuilder)
		{
			var number = 0;
			for (var i = 0; i < stringBuilder.Length; i++)
			{
				if (i > 0 && stringBuilder[i - 1] != '\n')
					continue;

				if (char.IsDigit(stringBuilder[i]))
					number++;
			}

			return number + 1;
		}

		private static string FormatLogMessage(string message, Func<string, string> formatter)
		{
			return formatter?.Invoke(message) ?? message;
		}

		private static bool CanBeEmpty(FieldInfo field)
		{
			return field.GetCustomAttribute<CanBeEmptyAttribute>() != null ||
				field.GetCustomAttribute<JetBrains.Annotations.CanBeNullAttribute>() != null ||
				field.GetCustomAttribute<System.Diagnostics.CodeAnalysis.MaybeNullAttribute>() != null;
		}

		private static bool FailIfEmpty(FieldInfo field)
		{
			return field.GetCustomAttribute<NotEmptyAttribute>() != null ||
				field.GetCustomAttribute<JetBrains.Annotations.NotNullAttribute>() != null ||
				field.GetCustomAttribute<System.Diagnostics.CodeAnalysis.NotNullAttribute>() != null;
		}

		private static IEnumerable<FieldInfo> GetSerializableFields(Type type)
		{
			while (type != null && type != typeof(object))
			{
				foreach (var field in type.GetFields(BindingFlags.Instance |
					BindingFlags.Public |
					BindingFlags.NonPublic |
					BindingFlags.DeclaredOnly))
				{
					if (IsSerializableField(field))
						yield return field;
				}

				type = type.BaseType;
			}
		}

		private static bool IsSerializableField(FieldInfo field)
		{
			if (field.IsStatic || field.IsInitOnly || field.IsLiteral || field.IsNotSerialized)
				return false;

			if (field.IsPublic)
				return true;

			return field.GetCustomAttribute<SerializeField>() != null ||
				field.GetCustomAttribute<SerializeReference>() != null;
		}

		private static bool IsTerminal(Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;

			return type.IsPrimitive ||
				type.IsEnum ||
				type == typeof(string) ||
				type == typeof(decimal) ||
				type == typeof(DateTime) ||
				type == typeof(TimeSpan) ||
				type == typeof(Guid) ||
				type == typeof(SerializableGuid);
		}

		private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
		{
			public static readonly ReferenceEqualityComparer Instance = new();

			public new bool Equals(object x, object y) => ReferenceEquals(x, y);

			public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
		}
	}
}
