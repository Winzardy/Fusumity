#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Content.ScriptableObjects;
using Fusumity.Editor.Utility;
using JetBrains.Annotations;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sapientia.Reflection;
using UnityEditor;

namespace Content.Editor
{
	using UnityObject = UnityEngine.Object;

	public static partial class ContentEntryEditorUtility
	{
		private static readonly Dictionary<SerializableGuid, IContentEntryScriptableObject> _guidToSource = new();

		private static readonly Dictionary<string, SerializableGuid> _tracking = new();
		private static readonly Dictionary<SerializableGuid, string> _trackingByGuid = new();
		private static readonly Dictionary<string, int> _arrayLengthByPath = new();
		private static readonly HashSet<string> _changedArrayPaths = new();
		private static readonly HashSet<int> _scheduledRefreshAssetIds = new();
		private static readonly List<ContentScriptableObject> _scheduledRefreshAssets = new();

		private static bool _scheduledRefreshAndSave;

		public static void ClearCache()
		{
			_guidToSource.Clear();
			_tracking.Clear();
			_trackingByGuid.Clear();
			_arrayLengthByPath.Clear();
			_changedArrayPaths.Clear();
			_scheduledRefreshAssetIds.Clear();
			_scheduledRefreshAssets.Clear();
			EditorApplication.delayCall -= HandleScheduledNestedRefresh;
		}

		public static void Refresh(this ContentScriptableObject asset, bool refreshAndSave = false)
		{
			if (asset is not IContentEntryScriptableObject scriptableObject
				|| scriptableObject.ScriptableContentEntry == null)
				return;

			if (!asset)
			{
				ContentDebug.LogWarning("ScriptableObject is null!", asset);
				return;
			}

			var dictionary = scriptableObject.ScriptableContentEntry.Nested;

			if (dictionary == null)
				return;

			using var so = new SerializedObject(asset);
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
			if (!key.target || guid == SerializableGuid.Empty)
				return false;

			var hash = GetTrackingHash(key.target, key.reference);
			if (_tracking.TryGetValue(hash, out var trackedGuid))
			{
				if (trackedGuid == guid)
				{
					_trackingByGuid[guid] = hash;
					return false;
				}

				if (_trackingByGuid.TryGetValue(trackedGuid, out var trackedHash)
					&& trackedHash == hash)
					_trackingByGuid.Remove(trackedGuid);

				_tracking[hash]       = guid;
				_trackingByGuid[guid] = hash;
				return true;
			}

			if (_trackingByGuid.TryGetValue(guid, out var previousHash)
				&& previousHash != hash)
				_tracking.Remove(previousHash);

			_tracking[hash]       = guid;
			_trackingByGuid[guid] = hash;
			return true;
		}

		public static bool Untrack(in (ContentScriptableObject asset, MemberReflectionReference<IUniqueContentEntry> reference) key)
		{
			if (!key.asset)
				return false;

			var hash = GetTrackingHash(key.asset, key.reference);
			if (_tracking.TryGetValue(hash, out var guid))
			{
				if (_trackingByGuid.TryGetValue(guid, out var trackedHash)
					&& trackedHash == hash)
					_trackingByGuid.Remove(guid);
			}

			return _tracking.Remove(hash);
		}

		public static bool TryGet(in (ContentScriptableObject asset, MemberReflectionReference<IUniqueContentEntry> reference) key,
			out SerializableGuid guid)
		{
			var hash = GetTrackingHash(key.asset, key.reference);
			return _tracking.TryGetValue(hash, out guid);
		}

		public static bool TrackArrayLength(ContentScriptableObject asset, string arrayPath, int length)
		{
			var hash = GetTrackingHash(asset, arrayPath);
			if (!_arrayLengthByPath.TryGetValue(hash, out var trackedLength))
			{
				_arrayLengthByPath[hash] = length;
				return _changedArrayPaths.Contains(hash);
			}

			if (trackedLength == length)
				return _changedArrayPaths.Contains(hash);

			_arrayLengthByPath[hash] = length;
			_changedArrayPaths.Add(hash);
			ScheduleNestedRefresh(asset);
			return true;
		}

		public static void ScheduleNestedRefresh(ContentScriptableObject asset)
			=> ScheduleNestedRefreshInternal(asset);

		private static string GetTrackingHash(ContentScriptableObject asset,
			MemberReflectionReference<IUniqueContentEntry> reference)
			=> GetTrackingHash(asset, reference.Path);

		private static string GetTrackingHash(ContentScriptableObject asset, string path)
			=> $"{asset.GetInstanceID()}:{path}";

		private static void ScheduleNestedRefreshInternal(ContentScriptableObject asset)
		{
			if (!asset)
				return;

			if (!_scheduledRefreshAssetIds.Add(asset.GetInstanceID()))
				return;

			_scheduledRefreshAssets.Add(asset);
			EditorApplication.delayCall -= HandleScheduledNestedRefresh;
			EditorApplication.delayCall += HandleScheduledNestedRefresh;
		}

		private static void HandleScheduledNestedRefresh()
		{
			try
			{
				foreach (var asset in _scheduledRefreshAssets)
				{
					if (asset)
						asset.Refresh(false);
				}
			}
			finally
			{
				_scheduledRefreshAssetIds.Clear();
				_scheduledRefreshAssets.Clear();
				_changedArrayPaths.Clear();
			}
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

			// SerializedObject держит нативную память до Dispose — если создали сами, обязаны освободить
			var ownsSerializedObject = serializedObject == null;
			serializedObject ??= new SerializedObject(asset);

			try
			{
				// Разрезаем алиасинг нативных [SerializeReference]: если один объект с вложенными
				// ContentEntry достижим по нескольким путям (несколько полей ссылаются на один rid),
				// клонируем его для лишних ссылок — иначе guid'ы конфликтуют и регенерятся на каждый Refresh
				if (DeAliasSharedContentReferences(asset, serializedObject))
					serializedObject.Update();

				var iterator = serializedObject.GetIterator();
				if (!iterator.Next(true))
					return;
				do
				{
					var modification = iterator.TryStartModificationNestedContentEntry(out var entry);

					if (!modification || entry is IScriptableContentEntry)
						continue;

					var reference = iterator.ToContentReference();
					if (reference.IsEmpty())
						continue;

					if (regenerateGuid)
						RegenerateGuid(entry, iterator.propertyPath, asset, false);

					map.SetOrAdd(reference, entry.Guid);
				} while (iterator.Next(true));

				scriptableObject.ScriptableContentEntry.ClearNested();

				foreach (var reference in map.Keys)
				{
					ref var guid = ref map[reference];
					if (!scriptableObject.ScriptableContentEntry!.RegisterNestedEntry(guid, reference))
					{
						if (IsAlias(scriptableObject.ScriptableContentEntry, in guid, in reference))
						{
							Track((asset, reference), in guid);
							continue;
						}

						var entry = reference.Resolve(scriptableObject.ScriptableContentEntry);
						guid = RegenerateGuid(entry, reference.Path, asset, false);
						scriptableObject.ScriptableContentEntry.RegisterNestedEntry(guid, reference);
					}

					Track((asset, reference), in guid);
				}

				SetDirty(serializedObject, refreshAndSave);
			}
			finally
			{
				map.ReleaseToStaticPool();

				if (ownsSerializedObject)
					serializedObject.Dispose();
			}

			if (log && ContentDebug.Logging.Nested.refresh)
			{
				var collection = scriptableObject.ScriptableContentEntry
					.Nested
					.GetCompositeString(x => $"[	{x.Key}	 ] {x.Value}",
						true);
				ContentDebug.Log($"Nested entries refreshed for source [ {asset.name} ]:" +
					$" {collection}", scriptableObject);
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

		/// <summary>
		/// Проверяет, что <paramref name="reference"/> и уже зарегистрированный под тем же
		/// <paramref name="guid"/> путь резолвятся в один и тот же объект (алиасинг общего
		/// SerializeReference rid), а не в две разные сущности с совпавшим guid
		/// </summary>
		private static bool IsAlias(IScriptableContentEntry scriptableEntry, in SerializableGuid guid,
			in MemberReflectionReference<IUniqueContentEntry> reference)
		{
			if (!scriptableEntry.TryGetNestedEntryReference(in guid, out var registeredReference))
				return false;

			// Сбрасываем кэш: ResolveSafe иначе вернёт прогретый _cache, игнорируя scriptableEntry
			registeredReference.CacheClear();
			var registeredEntry = registeredReference.ResolveSafe(scriptableEntry);

			var localReference = reference;
			localReference.CacheClear();
			var entry = localReference.ResolveSafe(scriptableEntry);

			return registeredEntry != null && ReferenceEquals(registeredEntry, entry);
		}

		/// <summary>
		/// Разрезает алиасинг нативных [SerializeReference]: если один managed-reference объект,
		/// содержащий вложенные ContentEntry, достижим по нескольким путям (несколько полей
		/// ссылаются на один rid), клонирует его для всех «лишних» ссылок. Так у каждого пути
		/// появляется свой экземпляр, и guid'ы вложенных записей перестают конфликтовать
		/// </summary>
		private static bool DeAliasSharedContentReferences(ContentScriptableObject asset, SerializedObject serializedObject)
		{
			const int safetyLimit = 4096;

			var changedAny = false;

			for (var iteration = 0; iteration < safetyLimit; iteration++)
			{
				serializedObject.Update();

				// rid -> пути полей, которые на него ссылаются (>1 = алиасинг)
				var ridToFieldPaths = new Dictionary<long, List<string>>();
				var contentEntryPaths = new List<string>();

				var iterator = serializedObject.GetIterator();
				if (iterator.Next(true))
				{
					do
					{
						if (iterator.propertyType == SerializedPropertyType.ManagedReference)
						{
							var id = iterator.managedReferenceId;
							if (id >= 0)
							{
								if (!ridToFieldPaths.TryGetValue(id, out var paths))
									ridToFieldPaths[id] = paths = new List<string>();

								paths.Add(iterator.propertyPath);
							}
						}

						if (iterator.TryStartModificationNestedContentEntry(out _))
							contentEntryPaths.Add(iterator.propertyPath);
					} while (iterator.Next(true));
				}

				// Берём самый внешний общий ref, под которым есть ContentEntry
				List<string> targetPaths = null;
				var bestDepth = int.MaxValue;

				foreach (var paths in ridToFieldPaths.Values)
				{
					if (paths.Count < 2)
						continue;

					if (!SubtreeHasContentEntry(paths[0], contentEntryPaths))
						continue;

					var depth = PathDepth(paths[0]);
					if (depth < bestDepth)
					{
						bestDepth = depth;
						targetPaths = paths;
					}
				}

				if (targetPaths == null)
					break;

				// Первую ссылку оставляем, остальные клонируем в независимые экземпляры
				var clonedThisIteration = false;
				for (var i = 1; i < targetPaths.Count; i++)
				{
					var prop = serializedObject.FindProperty(targetPaths[i]);
					if (prop == null || prop.propertyType != SerializedPropertyType.ManagedReference)
						continue;

					var original = prop.managedReferenceValue;
					if (original == null)
						continue;

					prop.managedReferenceValue = Sirenix.Serialization.SerializationUtility.CreateCopy(original);
					serializedObject.ApplyModifiedProperties();
					clonedThisIteration = true;
					changedAny = true;

					if (ContentDebug.Logging.Nested.regenerate)
						ContentDebug.LogWarning(
							$"<b>De-aliased</b> shared [SerializeReference] at path: <u>{targetPaths[i]}</u>", asset);
				}

				// Если разрезать не удалось — выходим, чтобы не зациклиться
				if (!clonedThisIteration)
					break;
			}

			if (changedAny)
				EditorUtility.SetDirty(asset);

			return changedAny;
		}

		private static bool SubtreeHasContentEntry(string head, List<string> contentEntryPaths)
		{
			foreach (var path in contentEntryPaths)
			{
				if (path.Length > head.Length
					&& path[head.Length] == '.'
					&& path.StartsWith(head, StringComparison.Ordinal))
					return true;
			}

			return false;
		}

		private static int PathDepth(string path)
		{
			var depth = 0;
			foreach (var symbol in path)
			{
				if (symbol == '.')
					depth++;
			}

			return depth;
		}

		public static SerializableGuid RegenerateGuid(IUniqueContentEntry entry, string path, UnityObject source, bool refreshAndSave = true)
		{
			var prevEntryGuid = entry.Guid;
			var newGuid = entry.RegenerateGuid();
			EditorUtility.SetDirty(source);

			if (refreshAndSave)
				ScheduleRefreshAndSave();

			if (ContentDebug.Logging.Nested.regenerate)
			{
				var msg = $"<b>Regenerated</b> guid [ {entry.Guid}]";
				if (prevEntryGuid != SerializableGuid.Empty)
					msg += $" from [ {prevEntryGuid} ]";
				msg += " for content entry by path: <u>" + path + "</u>";
				ContentDebug.LogWarning(msg, source);
			}

			return newGuid;
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

			EditorApplication.delayCall += HandleDelayCall;

			void HandleDelayCall()
			{
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
