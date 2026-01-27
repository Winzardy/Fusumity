#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Content.ScriptableObjects;
using Fusumity.Editor.Utility;
using JetBrains.Annotations;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Extensions.Reflection;
using Sapientia.Pooling;
using Sapientia.Reflection;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	using UnityObject = UnityEngine.Object;

	public static partial class ContentEntryEditorUtility
	{
		private static readonly Dictionary<SerializableGuid, IContentEntryScriptableObject> _guidToSource = new();

		private static readonly Dictionary<string, SerializableGuid> _tracking = new();
		private static readonly HashSet<SerializableGuid> _trackingByGuid = new();

		private static bool _scheduledRefreshAndSave;

		public static void ClearCache()
		{
			_guidToSource.Clear();
			_tracking.Clear();
			_trackingByGuid.Clear();
		}

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
			Refresh(asset, so, refreshAndSave, false, log: true);
		}

		/// <returns><c>true</c>, если запомнили без проблем; иначе <c>false</c></returns>
		public static bool Remember(this IContentEntryScriptableObject source, in SerializableGuid guid)
		{
			if (guid == SerializableGuid.Empty)
				return false;

			if (_guidToSource.TryAdd(guid, source))
				return true;

			var sourceByGuid = _guidToSource[guid];

			if (sourceByGuid.TimeCreated < source.TimeCreated)
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
			if (!_trackingByGuid.Add(guid))
				return false;

			var hash = $"{key.target.GetInstanceID()}:{key.reference.Path}";
			return _tracking.TryAdd(hash, guid);
		}

		public static bool Untrack(in (ContentScriptableObject asset, MemberReflectionReference<IUniqueContentEntry> reference) key)
		{
			if (!key.asset)
				return false;

			var hash = $"{key.asset.GetInstanceID()}:{key.reference.Path}";
			if (_tracking.TryGetValue(hash, out var guid))
				_trackingByGuid.Remove(guid);

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
			using var steps = new SimpleList<MemberReferencePathStep>();
			using (ListPool<int>.Get(out var skip))
			{
				for (int i = 0; i < reference.steps.Length; i++)
				{
					if (reference.steps[i].name == ContentConstants.CUSTOM_VALUE_FIELD_NAME)
						reference.steps[i].name = ContentConstants.VALUE_FIELD_NAME;

					// if (reference.steps[i].name == ContentConstants.UNITY_VALUE_FIELD_NAME)
					// 	skip.Add(i);
				}

				if (!skip.IsEmpty())
				{
					for (int i = 0; i < reference.steps.Length; i++)
					{
						if (skip.Contains(i))
							continue;

						steps.Add(in reference.steps[i]);
					}

					reference.steps = steps.ToArray();
				}
			}

			return reference;
		}

		public static void RecursiveRegenerateAndRefresh(this ContentScriptableObject asset, bool refreshAndSave = true)
		{
			Refresh(asset, null, refreshAndSave, true);
		}

		public static void Refresh(this ContentScriptableObject asset, [CanBeNull] SerializedObject serializedObject,
			bool refreshAndSave = true,
			bool regenerateGuid = false,
			bool log = false)
		{
			if (asset is not IContentEntryScriptableObject scriptableObject)
				return;

			var map = HashMapPool<MemberReflectionReference<IUniqueContentEntry>, SerializableGuid>.Get();

			serializedObject ??= new SerializedObject(asset);
			var iterator = serializedObject.GetIterator();
			if (!iterator.Next(true))
				return;
			do
			{
				var modification = iterator.TryStartModificationNestedContentEntry(out var entry);

				if (!modification || entry is IScriptableContentEntry)
					continue;

				var reference = iterator.ToContentReference();
				if (reference == null)
					continue;

				if (regenerateGuid)
					RegenerateGuid(entry, iterator.propertyPath, asset, false);

				map.SetOrAdd(reference, entry.Guid);

				Track((asset, reference), in entry.Guid);
			} while (iterator.Next(true));

			SetDirty(serializedObject, refreshAndSave);

			var skip = 1;

			if (!map.IsEmpty())
				EditorApplication.delayCall += HandleDelayCall;

			void HandleDelayCall()
			{
				if (skip > 0)
				{
					skip--;
					EditorApplication.delayCall += HandleDelayCall;
					return;
				}

				scriptableObject.ScriptableContentEntry.ClearNested();

				foreach (var reference in map.Keys)
				{
					ref var guid = ref map[reference];
					if (!scriptableObject.ScriptableContentEntry.RegisterNestedEntry(guid, reference))
						throw new Exception($"Can't add nested entry with guid [ {guid} ] by path [ {reference.Path} ]");
				}

				map.ReleaseToStaticPool();

				if (log && ContentDebug.Logging.Nested.refresh)
				{
					var collection = scriptableObject.ScriptableContentEntry.Nested
						.GetCompositeString(x => $"[	{x.Key}	 ] {x.Value}",
							true);
					ContentDebug.Log($"Nested entries refreshed for source [ {asset.name} ]:" +
						$" {collection}", scriptableObject);
				}
			}
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

		public static void RegenerateGuid(IUniqueContentEntry entry, string path, UnityObject source, bool refreshAndSave = true)
		{
			entry.RegenerateGuid();
			EditorUtility.SetDirty(source);

			if (refreshAndSave)
				ScheduleRefreshAndSave();

			var prevEntryGuid = entry.Guid;

			if (ContentDebug.Logging.Nested.regenerate)
			{
				var msg = $"<b>Regenerated</b> guid [ {entry.Guid}]";
				if (prevEntryGuid != SerializableGuid.Empty)
					msg += $" from [ {prevEntryGuid} ]";
				msg += " for content entry by path: <u>" + path + "</u>";
				ContentDebug.LogWarning(msg, source);
			}
		}

		private static void RestoreGuid(IUniqueContentEntry entry, in SerializableGuid guid, string path, UnityObject source)
		{
			var prev = entry.Guid;
			entry.SetGuid(in guid);
			EditorUtility.SetDirty(source);

			if (ContentDebug.Logging.Nested.restore)
				ContentDebug.LogWarning($"<b>Restored</b> guid [ {entry.Guid} ] from [ {prev} ]" +
					$" for content entry by path: <u>" + path + "</u>",
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
			if (ContentEditorCache.IsRebuilding)
				return;

			ContentEditorCache.IsRebuilding = true;
			var skip = 2;
			EditorApplication.delayCall += HandleDelayCall;

			void HandleDelayCall()
			{
				if (skip > 0)
				{
					skip--;
					EditorApplication.delayCall += HandleDelayCall;

					return;
				}

				AssetDatabase.Refresh();
				AssetDatabase.SaveAssets();
				ContentEditorCache.IsRebuilding = false;
			}
		}

		private static bool TryStartModificationNestedContentEntry(this SerializedProperty property,
			out IUniqueContentEntry entry)
		{
			entry = null;

			if (property == null)
				return false;

			if (property.isArray)
				return false;

			if (property.propertyType != SerializedPropertyType.Generic)
				return false;

			if (!property.type.StartsWith("ContentEntry")) // фильтруем только ContentEntry
				return false;

			var value = property.GetValueByReflectionSafe();
			if (value is not IUniqueContentEntry contentEntry)
				return false;

			entry = contentEntry;
			return true;
		}
	}
}
#endif
