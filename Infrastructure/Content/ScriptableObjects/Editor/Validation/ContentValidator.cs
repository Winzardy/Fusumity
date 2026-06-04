using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Content.ScriptableObjects;
using Content.ScriptableObjects.Editor;
using Sapientia;
using Sapientia.Extensions;
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
			return Validate(SyncBeforeValidate);
		}

		public static bool TryGetValidator<T>(out T validator)
			where T : class, IContentValueValidator
		{
			validator = ContentValidationSettings.Settings.GetEnabledCustomValidator<T>();
			return validator != null;
		}

		public static bool Validate(bool sync)
		{
			_cancelRequested = false;

			if (sync)
				ContentDatabaseEditorUtility.SyncContent();

			if (IsValidationCancellationRequested())
				return CancelValidation();

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
				foreach (var database in dbs)
				{
					if (database.scriptableObjects == null)
					{
						errorCount++;
						ContentDebug.LogError($"Null scriptable objects list in database [ {database.name} ]", database);
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
								return CancelValidation("user");
							}
						}

						if (IsValidationCancellationRequested())
							return CancelValidation();

						progress++;

						if (!scriptableObject)
						{
							errorCount++;
							ContentDebug.LogError($"Null scriptable object in database [ {database.name} ]", database);
							continue;
						}

						if (scriptableObject.SkipValidation())
							continue;

						if (scriptableObject is IValidatable validatable)
						{
							if (!validatable.Validate(out var message))
							{
								errorCount++;
								ContentDebug.LogError(message, scriptableObject);
							}
						}

						errorCount += ValidateContentReferences(scriptableObject, valueValidators, ref warningCount);
					}
				}

				if (errorCount > 0)
				{
					ContentDebug.LogError($"Validation failed (errors: {errorCount}, warnings️: {warningCount})");
					return false;
				}

				ContentDebug.Log(warningCount > 0
					? $"Validation passed (warnings️: {warningCount})"
					: "Validation passed");
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
			return ContentValidationSettings.Settings.GetEnabledCustomValidators();
		}

		public static int ValidateContentObject(object target,
			Type targetType,
			string path,
			ContentScriptableObject source,
			object logContext,
			IReadOnlyList<IContentValueValidator> valueValidators,
			ref int warningCount,
			bool inspectUnityObject = false,
			Func<string, string> logMessageFormatter = null)
		{
			var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
			return ValidateContentReferences(target,
				targetType,
				path,
				source,
				logContext ?? source,
				visited,
				false,
				false,
				valueValidators,
				ref warningCount,
				inspectUnityObject,
				logMessageFormatter);
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
			ref int warningCount)
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
				null);
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
			Func<string, string> logMessageFormatter)
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
				logMessageFormatter);
			if (target == null)
				return errorCount;

			if (target is IContentReference reference)
				return errorCount + ValidateContentReference(reference,
					path,
					logContext,
					canBeEmpty,
					failIfEmpty,
					ref warningCount,
					logMessageFormatter);

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
					logMessageFormatter);

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
					logMessageFormatter);

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
					ContentDebug.LogError(
						FormatLogMessage($"Content validation: can't read field [ {path}.{field.Name} ]: {e.Message}",
							logMessageFormatter),
						logContext);
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
					logMessageFormatter);
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
			Func<string, string> logMessageFormatter)
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
					ContentDebug.LogError(
						FormatLogMessage(
							$"Content value validator [ {validator.GetType().Name} ] failed at [ {path} ]: {e.Message}",
							logMessageFormatter),
						logContext);
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

				ContentDebug.LogError(FormatLogMessage(message, logMessageFormatter), logContext);
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
			Func<string, string> logMessageFormatter)
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
					logMessageFormatter);

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
					logMessageFormatter);
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
			Func<string, string> logMessageFormatter)
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
					logMessageFormatter);
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
			Func<string, string> logMessageFormatter)
		{
			if (reference.IsEmpty())
			{
				if (failIfEmpty)
				{
					ContentDebug.LogError(
						FormatLogMessage($"Empty content reference [ {path} ] with [NotEmpty] or [NotNull] attribute",
							logMessageFormatter),
						logContext);
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

			ContentDebug.LogError(
				FormatLogMessage($"Invalid content reference [ {path} ] by type [ {valueType.Name} ] and guid [ {reference.Guid} ]",
					logMessageFormatter),
				logContext);
			return 1;
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
