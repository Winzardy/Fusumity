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
using Sapientia.Utility;
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
		private static ContentValidationReport _activeReport;
		private static bool _cancelRequested;

		internal static ContentValidationReport LastReport { get; private set; }

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
			if (_activeReport != null)
				return ValidateInternal(sync, out errorOrMessage);

			ClearLastReport();
			var report = Pool<ContentValidationReport>.Get();
			LastReport = report;
			_activeReport = report;

			var result = false;
			try
			{
				result = ValidateInternal(sync, out errorOrMessage);
				return result;
			}
			finally
			{
				report.Complete(result && !report.WasCanceled);
				_activeReport = null;

				if (!Application.isBatchMode)
					ContentValidationReportWindow.ShowAfterValidation();
			}
		}

		internal static void ClearLastReport()
		{
			if (LastReport == null || ReferenceEquals(LastReport, _activeReport))
				return;

			var report = LastReport;
			LastReport = null;
			Pool<ContentValidationReport>.Release(report);
		}

		private static bool ValidateInternal(bool sync, out string errorOrMessage)
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
								RecordError(errorMessage,
									database,
									$"{database.name}.{nameof(ContentDatabaseScriptableObject.scriptableObjects)}");
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
									RecordError(errorMessage,
										database,
										$"{database.name}.{nameof(ContentDatabaseScriptableObject.scriptableObjects)}");
									continue;
								}

								if (!scriptableObject.Enabled || scriptableObject.SkipValidation())
									continue;

								if (scriptableObject is IValidatable validatable)
								{
									if (!validatable.Validate(out var soMessage))
									{
										errorCount++;
										AppendErrorMessage(errStringBuilder, soMessage);
										RecordError(soMessage, scriptableObject, scriptableObject.name);
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
							errorOrMessage = "Content validation " + str;
							return false;
						}

						errorOrMessage = warningCount > 0
							? $"Validation passed (warnings️: {warningCount})"
							: "Validation passed";
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
			_activeReport?.Cancel();
			RecordWarning($"Validation canceled ({reason})", path: TITLE);
			return false;
		}

		private static void RecordError(string message, object context = null, string path = null)
		{
			_activeReport?.AddError(message, context as UnityObject, path);
		}

		private static void RecordWarning(string message, object context = null, string path = null)
		{
			_activeReport?.AddWarning(message, context as UnityObject, path);
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
			StringBuilder errorMessageBuilder,
			ContentReferenceAttribute contentReferenceAttribute = null)
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
				errorMessageBuilder,
				contentReferenceAttribute);
			errorCount += ValidateContentReferenceAttribute(target,
				path,
				contentReferenceAttribute,
				logContext,
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

			if (target is UnityObject && !inspectUnityObject && !ReferenceEquals(target, context))
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
					errorMessageBuilder,
					contentReferenceAttribute);

			if (target is IList enumerable)
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
					errorMessageBuilder,
					contentReferenceAttribute);

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
					RecordError(errorMessage, logContext, $"{path}.{field.Name}");
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
					errorMessageBuilder,
					field.GetCustomAttribute<ContentReferenceAttribute>());
			}

			return errorCount;
		}

		private static int ValidateContentReferenceAttribute(object value,
			string path,
			ContentReferenceAttribute attribute,
			object logContext,
			Func<string, string> logMessageFormatter,
			StringBuilder errorMessageBuilder)
		{
			if (attribute == null || value is IContentReference)
				return 0;

			var valueTypeForReference = attribute.Type;
			if (valueTypeForReference == null && !attribute.TypeName.IsNullOrEmpty())
				ReflectionUtility.TryGetType(attribute.TypeName, out valueTypeForReference);

			if (valueTypeForReference == null || value == null)
				return 0;

			if (value is string id)
			{
				if (id.IsNullOrEmpty())
					return 0;

				if (ContentEditorCache.TryGetSource(valueTypeForReference, id, out var source))
					return ValidateContentReferenceSource(source,
						path,
						valueTypeForReference,
						$"id [ {id} ]",
						logContext,
						logMessageFormatter,
						errorMessageBuilder);

				return AddInvalidContentReferenceError(path,
					valueTypeForReference,
					$"id [ {id} ]",
					logContext,
					logMessageFormatter,
					errorMessageBuilder);
			}

			if (value is SerializableGuid guid && guid != SerializableGuid.Empty)
			{
				if (ContentEditorCache.TryGetSource(valueTypeForReference, in guid, out var source))
					return ValidateContentReferenceSource(source,
						path,
						valueTypeForReference,
						$"guid [ {guid} ]",
						logContext,
						logMessageFormatter,
						errorMessageBuilder);

				return AddInvalidContentReferenceError(path,
					valueTypeForReference,
					$"guid [ {guid} ]",
					logContext,
					logMessageFormatter,
					errorMessageBuilder);
			}

			return 0;
		}

		private static int ValidateContentReferenceSource(IContentEntrySource source,
			string path,
			Type valueType,
			string identifier,
			object logContext,
			Func<string, string> logMessageFormatter,
			StringBuilder errorMessageBuilder)
		{
			if (!ContentEditorCache.IsSourceDisabled(source, out var target))
				return 0;

			var disabledReferenceMessage = FormatLogMessage(
				$"Content reference [ {path} ] points to disabled config [ {AssetDatabase.GetAssetPath(target)} ] " +
				$"by type [ {valueType.Name} ] and {identifier}",
				logMessageFormatter);
			AppendErrorMessage(errorMessageBuilder, disabledReferenceMessage);
			RecordError(disabledReferenceMessage, logContext, path);
			return 1;
		}

		private static int AddInvalidContentReferenceError(string path,
			Type valueType,
			string identifier,
			object logContext,
			Func<string, string> logMessageFormatter,
			StringBuilder errorMessageBuilder)
		{
			var invalidReferenceMessage = FormatLogMessage(
				$"Invalid content reference [ {path} ] by type [ {valueType.Name} ] and {identifier}",
				logMessageFormatter);
			AppendErrorMessage(errorMessageBuilder, invalidReferenceMessage);
			RecordError(invalidReferenceMessage, logContext, path);
			return 1;
		}

		private static int ValidateContentValue(object value,
			Type valueType,
			string path,
			ContentScriptableObject context,
			object logContext,
			bool canBeEmpty,
			IReadOnlyList<IContentValueValidator> validators,
			Func<string, string> logMessageFormatter,
			StringBuilder errorMessageBuilder,
			ContentReferenceAttribute contentReferenceAttribute = null)
		{
			if (validators == null || validators.Count == 0)
				return 0;

			var errorCount = 0;
			var validationContext = new ContentValidationContext(value,
				valueType,
				path,
				context,
				canBeEmpty,
				contentReferenceAttribute);
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
					RecordError(errorMessage, logContext, path);
					errorCount++;
					continue;
				}

				if (message == null)
				{
					var invalidMessage = $"Invalid content value [ {path} ] by type [ {valueType.Name} ] and validator [ {validator.GetType().Name} ]";
					AppendErrorMessage(errorMessageBuilder, invalidMessage);
					RecordError(invalidMessage, logContext, path);
					errorCount++;
					continue;
				}

				if (message.IsNullOrEmpty())
					message = $"Invalid content value [ {path} ] by type [ {valueType.Name} ] and validator [ {validator.GetType().Name} ]";

				var formattedMessage = FormatLogMessage(message, logMessageFormatter);
				AppendErrorMessage(errorMessageBuilder, formattedMessage);
				RecordError(formattedMessage, logContext, path);
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
			StringBuilder errorMessageBuilder,
			ContentReferenceAttribute contentReferenceAttribute = null)
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
					errorMessageBuilder,
					contentReferenceAttribute);

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
					errorMessageBuilder,
					contentReferenceAttribute);
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
			StringBuilder errorMessageBuilder,
			ContentReferenceAttribute contentReferenceAttribute = null)
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
					errorMessageBuilder,
					contentReferenceAttribute);
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
					RecordError(errorMessage, logContext, path);
					return 1;
				}

				if (!canBeEmpty)
				{
					warningCount++;
					RecordWarning(
						FormatLogMessage($"Empty content reference [ {path} ] without [CanBeEmpty], [CanBeNull] or [MaybeNull] attribute",
							logMessageFormatter),
						logContext,
						path);
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

			if (ContentEditorCache.TryGetSource(reference, valueType, out var source) &&
				ContentEditorCache.IsSourceDisabled(source, out var target))
			{
				var disabledReferenceMessage = FormatLogMessage(
					$"Content reference [ {path} ] points to disabled config [ {AssetDatabase.GetAssetPath(target)} ] " +
					$"by type [ {valueType.Name} ] and guid [ {reference.Guid} ]",
					logMessageFormatter);
				AppendErrorMessage(errorMessageBuilder, disabledReferenceMessage);
				RecordError(disabledReferenceMessage, logContext, path);
				return 1;
			}

			if (reference.IsValid())
				return 0;

			var invalidReferenceMessage = FormatLogMessage($"Invalid content reference [ {path} ] by type [ {valueType.Name} ] and guid [ {reference.Guid} ]",
				logMessageFormatter);
			AppendErrorMessage(errorMessageBuilder, invalidReferenceMessage);
			RecordError(invalidReferenceMessage, logContext, path);
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

	internal enum ContentValidationSeverity
	{
		Error,
		Warning
	}

	internal readonly struct ContentValidationReportEntry
	{
		public ContentValidationSeverity Severity { get; }
		public string Message { get; }
		public UnityObject Context { get; }
		public string Path { get; }

		public ContentValidationReportEntry(
			ContentValidationSeverity severity,
			string message,
			UnityObject context,
			string path)
		{
			Severity = severity;
			Message = message.IsNullOrEmpty()
				? "Content validation failed without a message"
				: message;
			Context = context;
			Path = path ?? string.Empty;
		}
	}

	internal sealed class ContentValidationReport : IPoolable
	{
		private readonly List<ContentValidationReportEntry> _entries = new();

		public IReadOnlyList<ContentValidationReportEntry> Entries => _entries;
		public int ErrorCount { get; private set; }
		public int WarningCount { get; private set; }
		public int Generation { get; private set; }
		public bool IsComplete { get; private set; }
		public bool IsValid { get; private set; }
		public bool WasCanceled { get; private set; }

		internal void AddError(string message, UnityObject context, string path)
		{
			_entries.Add(new ContentValidationReportEntry(ContentValidationSeverity.Error, message, context, path));
			ErrorCount++;
		}

		internal void AddWarning(string message, UnityObject context, string path)
		{
			_entries.Add(new ContentValidationReportEntry(ContentValidationSeverity.Warning, message, context, path));
			WarningCount++;
		}

		internal void Cancel()
		{
			WasCanceled = true;
		}

		internal void Complete(bool isValid)
		{
			IsComplete = true;
			IsValid = isValid;
		}

		void IPoolable.OnGet()
		{
			Generation++;
		}

		void IPoolable.Release()
		{
			_entries.Clear();
			ErrorCount = 0;
			WarningCount = 0;
			IsComplete = false;
			IsValid = false;
			WasCanceled = false;
		}
	}
}
