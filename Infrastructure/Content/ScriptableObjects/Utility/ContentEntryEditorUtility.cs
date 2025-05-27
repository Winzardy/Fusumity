#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Content.ScriptableObjects;
using Fusumity.Editor.Utility;
using JetBrains.Annotations;
using Sapientia.Extensions;
using Sapientia.Reflection;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Content.Editor
{
	using UnityObject = Object;

	public static partial class ContentEntryEditorUtility
	{
		/// <summary>
		/// <see cref="ContentEntry{T}"/>
		/// </summary>
		private const string CONTENT_ENTRY_PREFIX = "ContentEntry";

		private static readonly Dictionary<SerializableGuid, IContentEntryScriptableObject> _guidToSource = new();

		//private static readonly Dictionary<SerializableGuid, IContentEntryScriptableObject> _guidToSource = new();
		private static readonly Dictionary<string, SerializableGuid> _tracking = new();

		private static bool _scheduledRefreshAndSave;

		public static void Refresh(this ContentScriptableObject asset, bool refreshAndSave = false)
		{
			if (asset is not IContentEntryScriptableObject scriptableObject)
				return;

			if (!asset)
			{
				ContentDebug.LogWarning("ScriptableObject is null!", asset);
				return;
			}

			var dictionary = scriptableObject.ScriptableContentEntry.Nested;

			if (dictionary == null)
				return;

			var so = new SerializedObject(asset);

			scriptableObject.ScriptableContentEntry.ClearNested();
			Refresh(asset, so, refreshAndSave);
			SetDirty(so, refreshAndSave);

			if (ContentDebug.Logging.Nested.refresh)
			{
				var collection = dictionary.GetCompositeString(true,
					x => $"[	{x.Key}	 ] {x.Value}");
				ContentDebug.Log($"Nested entries refreshed for source [ {asset.name} ]:" +
					$" {collection}", scriptableObject);
			}
		}

		private static void Refresh(ContentScriptableObject asset, SerializedObject serializedObject, bool refreshAndSave = true)
		{
			if (asset is not IContentEntryScriptableObject scriptableObject)
				return;

			var iterator = serializedObject.GetIterator();
			if (!iterator.NextVisible(true))
				return;
			do
			{
				using var modification = iterator.TryStartModificationEntry(out var entry);
				if (!modification)
					continue;

				var reference = iterator.ToContentReference();

				if (reference == null)
					continue;

				if (!scriptableObject.ScriptableContentEntry.RegisterNestedEntry(entry.Guid, reference))
				{
					iterator.RegenerateGuid(entry, asset, refreshAndSave);

					if (scriptableObject.ScriptableContentEntry.RegisterNestedEntry(entry.Guid, reference))
						SetDirty(serializedObject, refreshAndSave);
					else
						throw new ArgumentException($"{entry.Guid} is already registered (after regenerate?)");
				}
				else
				{
					EditorUtility.SetDirty(asset);
				}

				Track((asset, reference), in entry.Guid);
			} while (iterator.NextVisible(true));
		}

		/// <returns><c>true</c>, если запомнили без проблем; иначе <c>false</c></returns>
		public static bool Remember(this IContentEntryScriptableObject source, in SerializableGuid guid)
		{
			if (_guidToSource.TryAdd(guid, source))
				return true;

			var sourceByGuid = _guidToSource[guid];

			if (sourceByGuid.TimeCreated > source.TimeCreated)
				return false;

			_guidToSource[guid] = source;
			return true;
		}

		/// <summary>
		/// Назвал Track чтобы не путать с Register, идея в том, чтобы следить за парой (hash -> guid)
		/// </summary>
		public static bool Track(in (ContentScriptableObject target, MemberReflectionReference<IUniqueContentEntry> reference) key,
			in SerializableGuid guid)
		{
			var hash = $"{key.target.GetInstanceID()}:{key.reference.Path}";
			return _tracking.TryAdd(hash, guid);
		}

		public static bool Untrack(in (ContentScriptableObject asset, MemberReflectionReference<IUniqueContentEntry> reference) key)
		{
			if (!key.asset)
				return false;

			var hash = $"{key.asset.GetInstanceID()}:{key.reference.Path}";
			return _tracking.Remove(hash);
		}

		public static bool TryGet(in (ContentScriptableObject asset, MemberReflectionReference<IUniqueContentEntry> reference) key,
			out SerializableGuid guid)
		{
			var hash = $"{key.asset.GetInstanceID()}:{key.reference.Path}";
			return _tracking.TryGetValue(hash, out guid);
		}

		public static void ResolveCache(IContentEntryScriptableObject scriptable)
		{
			if (scriptable.ScriptableContentEntry.Nested == null)
				return;

			foreach (var reference in scriptable.ScriptableContentEntry.Nested.Values)
				reference.Resolve(scriptable.ScriptableContentEntry, true);
		}

		public static void ClearCache(IContentEntryScriptableObject scriptable)
		{
			if (scriptable.ScriptableContentEntry.Nested == null)
				return;

			foreach (var reference in scriptable.ScriptableContentEntry.Nested.Values)
				reference.CacheClear();
		}

		[CanBeNull]
		public static MemberReflectionReference<IUniqueContentEntry> ToContentReference(this SerializedProperty property)
		{
			//TODO: нужно чтобы Nested обязательно лежали в IContentEntry... (подумать)
			if (!property.propertyPath.Contains(IContentEntrySource.ENTRY_FIELD_NAME))
				return null;

			var reference = property.ToReference<IUniqueContentEntry>(1);
			return reference.FixSerializeReference();
		}

		private static MemberReflectionReference<IUniqueContentEntry> FixSerializeReference(
			this MemberReflectionReference<IUniqueContentEntry> reference)
		{
			for (int i = 0; i < reference.steps.Length; i++)
			{
				if (reference.steps[i].name == IContentEntry.UNITY_VALUE_FIELD_NAME ||
				    reference.steps[i].name == IContentEntry.CUSTOM_VALUE_FIELD_NAME)
					reference.steps[i].name = IContentEntry.VALUE_FIELD_NAME;
			}

			return reference;
		}

		public static void RecursiveRegenerateAndRefresh(this ContentScriptableObject asset, bool refreshAndSave = true)
		{
			if (asset is not IContentEntryScriptableObject scriptableObject)
				return;

			var serializedObject = new SerializedObject(asset);
			scriptableObject.ScriptableContentEntry.ClearNested();

			var iterator = serializedObject.GetIterator();
			if (!iterator.NextVisible(true))
				return;
			do
			{
				using var modification = iterator.TryStartModificationEntry(out var entry);
				if (!modification || entry is IScriptableContentEntry)
					continue;

				var reference = iterator.ToContentReference();
				if (reference == null)
					continue;

				iterator.RegenerateGuid(entry, asset, refreshAndSave);
				SetDirty(iterator.serializedObject, refreshAndSave);

				if (!scriptableObject.ScriptableContentEntry.RegisterNestedEntry(entry.Guid, reference))
					throw new Exception($"Can't add nested entry with guid [ {entry.Guid} ] by path [ {reference.Path} ]");

				Track((asset, reference), in entry.Guid);
			} while (iterator.NextVisible(true));
		}

		private static void ClearNested(this IScriptableContentEntry entry)
		{
			var dictionary = entry.Nested;

			foreach (var (guid, reference) in dictionary)
			{
				Untrack((entry.ScriptableObject, reference));
				_guidToSource.Remove(guid);
			}

			entry.ClearNestedCollection();
		}

		public static void RegenerateGuid(this SerializedProperty property,
			IUniqueContentEntry entry,
			ContentScriptableObject asset,
			bool refreshAndSave = true)
		{
			RegenerateGuid(entry, property.propertyPath, asset, refreshAndSave);
			RecursiveRegenerateGuidForChildren(property, asset, refreshAndSave);
		}

		private static void RecursiveRegenerateGuidForChildren(this SerializedProperty property, ContentScriptableObject asset,
			bool refreshAndSave = true)
		{
			var iterator = property.Copy();
			var depth = property.depth;

			if (!iterator.Next(true))
				return;

			do
			{
				if (iterator.depth <= depth)
					break;

				using var modification = iterator.TryStartModificationEntry(out var entry);
				// Попробуем получить объект из property
				if (!modification)
					continue;

				RegenerateGuid(entry, iterator.propertyPath, asset, refreshAndSave);
				SetDirty(iterator.serializedObject, refreshAndSave);
			} while (iterator.NextVisible(true));
		}

		public static void RegenerateGuid(IUniqueContentEntry entry, string path, UnityObject asset, bool refreshAndSave = true)
		{
			var prevEntryGuid = entry.Guid;

			entry.RegenerateGuid();
			EditorUtility.SetDirty(asset);
			if (refreshAndSave)
				ScheduleRefreshAndSave();

			if (ContentDebug.Logging.Nested.regenerate)
			{
				var msg = $"<b>Regenerated</b> guid [ {entry.Guid}]";
				if (prevEntryGuid != SerializableGuid.Empty)
					msg += $" from [ {prevEntryGuid} ]";
				msg += " for content entry by path: " + path;
				ContentDebug.LogWarning(msg, asset);
			}
		}

		private static void RestoreGuid(IUniqueContentEntry entry, in SerializableGuid guid, string path, UnityObject source)
		{
			var prev = entry.Guid;
			entry.SetGuid(in guid);
			EditorUtility.SetDirty(source);

			if (ContentDebug.Logging.Nested.restore)
				ContentDebug.LogWarning($"<b>Restored</b> guid [ {entry.Guid} ] from [ {prev} ] for content entry by path: " + path,
					source);
		}

		private static void SetDirty(SerializedObject serializedObject, bool refreshAndSave = true)
		{
			EditorUtility.SetDirty(serializedObject.targetObject);
			serializedObject.ApplyModifiedProperties();

			if (refreshAndSave)
				ScheduleRefreshAndSave();
		}

		private static void ScheduleRefreshAndSave()
		{
			if (_scheduledRefreshAndSave)
				return;

			_scheduledRefreshAndSave = true;
			EditorApplication.delayCall += OnDelayCall;

			void OnDelayCall()
			{
				AssetDatabase.Refresh();
				AssetDatabase.SaveAssets();
				_scheduledRefreshAndSave = false;
			}
		}

		private static SerializedPropertyModification TryStartModificationEntry(this SerializedProperty property,
			out IUniqueContentEntry entry)
		{
			entry = null;
			if (!property.type.StartsWith(CONTENT_ENTRY_PREFIX))
				return null;

			if (property.isArray)
				return null;

			var propertyValue = property.GetValueByReflection();
			if (propertyValue is not IUniqueContentEntry uniqueContentEntry)
				return null;

			entry = uniqueContentEntry;
			return entry == null ? null : new SerializedPropertyModification(property);
		}

		private class SerializedPropertyModification : IDisposable
		{
			private readonly SerializedProperty _property;

			public SerializedPropertyModification(SerializedProperty property)
			{
				_property = property;
			}

			public void Dispose()
			{
				_property.serializedObject.ApplyModifiedProperties();
			}

			public static implicit operator bool(SerializedPropertyModification modification) => modification != null;
		}
	}
}
#endif
