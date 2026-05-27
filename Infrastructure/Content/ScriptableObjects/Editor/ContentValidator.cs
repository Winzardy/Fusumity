using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Content.ScriptableObjects;
using Content.ScriptableObjects.Editor;
using Sapientia;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Content.Editor
{
	public static class ContentValidator
	{
		private const string TITLE = "Validate Content";

		private const string SYNC_BEFORE_VALIDATE_PREF_KEY = "ContentValidator.SyncBeforeValidate";
		private const string SYNC_BEFORE_VALIDATE_MENU = ContentMenuConstants.TOOLS_MENU + "Validate/Sync Before Validate";

		private static bool SyncBeforeValidate { get => EditorPrefs.GetBool(SYNC_BEFORE_VALIDATE_PREF_KEY, true); set => EditorPrefs.SetBool(SYNC_BEFORE_VALIDATE_PREF_KEY, value); }

		[MenuItem(SYNC_BEFORE_VALIDATE_MENU, priority = 100)]
		public static void ToggleSyncBeforeValidate()
		{
			SyncBeforeValidate = !SyncBeforeValidate;
			Menu.SetChecked(SYNC_BEFORE_VALIDATE_MENU, SyncBeforeValidate);
		}

		[MenuItem(SYNC_BEFORE_VALIDATE_MENU, true, priority = 100)]
		public static bool ToggleSyncBeforeValidateValidate()
		{
			Menu.SetChecked(SYNC_BEFORE_VALIDATE_MENU, SyncBeforeValidate);
			return true;
		}

		[MenuItem(ContentMenuConstants.TOOLS_MENU + "Validate/Run")]
		public static void Validate()
		{
			Validate(SyncBeforeValidate);
		}

		public static void Validate(bool sync)
		{
			if (sync)
				ContentDatabaseEditorUtility.SyncContent();

			var errorCount = 0;

			var dbs = ContentEditorCache.GetAssets<ContentDatabaseScriptableObject>()
				.ToList();

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
						if (!InternalEditorUtility.inBatchMode)
						{
							var scriptableObjectName = scriptableObject ? scriptableObject.name : "null";
							var progressValue = totalProgress > 0 ? progress / totalProgress : 1;
							EditorUtility.DisplayProgressBar(TITLE,
								$"{database.name} / {scriptableObjectName}",
								progressValue);
						}

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

						errorCount += ValidateContentReferences(scriptableObject);
					}
				}

				if (errorCount > 0)
				{
					ContentDebug.LogError($"Validation failed (errors: {errorCount})");
					if (InternalEditorUtility.inBatchMode)
						EditorApplication.Exit(1);

					return;
				}

				ContentDebug.Log("Validation passed");
			}
			finally
			{
				if (!InternalEditorUtility.inBatchMode)
					EditorUtility.ClearProgressBar();
			}
		}

		private static int ValidateContentReferences(ContentScriptableObject scriptableObject)
		{
			if (!scriptableObject)
				return 0;

			var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
			return ValidateContentReferences(scriptableObject,
				scriptableObject.GetType(),
				scriptableObject.name,
				scriptableObject,
				visited,
				false);
		}

		private static int ValidateContentReferences(object target,
			Type targetType,
			string path,
			ContentScriptableObject context,
			HashSet<object> visited,
			bool canBeEmpty)
		{
			if (target == null || targetType == null)
				return 0;

			if (target is IContentReference reference)
				return ValidateContentReference(reference, path, context, canBeEmpty);

			if (IsTerminal(targetType))
				return 0;

			if (target is UnityObject && target != context)
				return 0;

			if (targetType.IsClass && !visited.Add(target))
				return 0;

			if (target is IDictionary dictionary)
				return ValidateDictionary(dictionary, path, context, visited, canBeEmpty);

			if (target is IEnumerable enumerable && target is not string)
				return ValidateEnumerable(enumerable, path, context, visited, canBeEmpty);

			var errorCount = 0;
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
					ContentDebug.LogError($"Content validation: can't read field [ {path}.{field.Name} ]: {e.Message}", context);
					errorCount++;
					continue;
				}

				errorCount += ValidateContentReferences(value,
					value?.GetType() ?? field.FieldType,
					$"{path}.{field.Name}",
					context,
					visited,
					CanBeEmpty(field));
			}

			return errorCount;
		}

		private static int ValidateDictionary(IDictionary dictionary,
			string path,
			ContentScriptableObject context,
			HashSet<object> visited,
			bool canBeEmpty)
		{
			var errorCount = 0;
			foreach (DictionaryEntry entry in dictionary)
			{
				errorCount += ValidateContentReferences(entry.Key,
					entry.Key?.GetType(),
					$"{path}[key: {entry.Key}]",
					context,
					visited,
					canBeEmpty);

				errorCount += ValidateContentReferences(entry.Value,
					entry.Value?.GetType(),
					$"{path}[{entry.Key}]",
					context,
					visited,
					canBeEmpty);
			}

			return errorCount;
		}

		private static int ValidateEnumerable(IEnumerable enumerable,
			string path,
			ContentScriptableObject context,
			HashSet<object> visited,
			bool canBeEmpty)
		{
			var errorCount = 0;
			var index = 0;
			foreach (var item in enumerable)
			{
				errorCount += ValidateContentReferences(item,
					item?.GetType(),
					$"{path}[{index}]",
					context,
					visited,
					canBeEmpty);
				index++;
			}

			return errorCount;
		}

		private static int ValidateContentReference(IContentReference reference,
			string path,
			ContentScriptableObject context,
			bool canBeEmpty)
		{
			if (reference.IsEmpty())
			{
				if (!canBeEmpty)
					ContentDebug.LogWarning(
						$"Empty ContentReference [ {path} ] without [CanBeEmpty], [CanBeNull] or [MaybeNull] attribute",
						context);

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
				$"Invalid ContentReference [ {path} ] by type [ {valueType.Name} ] and guid [ {reference.Guid} ]",
				context);
			return 1;
		}

		private static bool CanBeEmpty(FieldInfo field)
		{
			return field.GetCustomAttribute<CanBeEmptyAttribute>() != null ||
				field.GetCustomAttribute<JetBrains.Annotations.CanBeNullAttribute>() != null ||
				field.GetCustomAttribute<System.Diagnostics.CodeAnalysis.MaybeNullAttribute>() != null;
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
