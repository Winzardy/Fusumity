using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fusumity.Editor.Utility;
using Sapientia;
using Sapientia.Collections;
using Sapientia.Pooling;
using UnityEngine;

namespace Content.Editor
{
	using UnityObject = UnityEngine.Object;

	public static class ContentEditorCache
	{
		public static bool IsRebuilding { get; set; }

		private static Dictionary<Type, int> _typeToVersion = new();

		private static Dictionary<string, ScriptableObject> _cache;
		private static Dictionary<string, HashSet<ScriptableObject>> _typeToCollection;

		internal static event Action Cleared;

		private static int _refreshCount;

		static ContentEditorCache() => Cleared += HandleCleared;

		private static void HandleCleared() => _typeToVersion.Clear();

		private static Dictionary<string, ScriptableObject> cache
		{
			get
			{
				if (_cache.IsNullOrEmpty())
					ClearAndRefreshScrObjs();

				return _cache;
			}
		}

		public static int version => cache.Count;

		public static void ClearAndRefreshScrObjs()
		{
			Cleared?.Invoke();

			if (!_typeToCollection.IsNullOrEmpty())
				foreach (var list in _typeToCollection.Values)
					list.ReleaseToStaticPool();
			_typeToCollection?.Clear();

			_cache ??= new();
			_cache.Clear();

			foreach (var scriptableObject in AssetDatabaseUtility.GetAssets<ScriptableObject>("ContentScriptableObject", null))
				Register(scriptableObject);
		}

		public static IEnumerable<T> GetAssets<T>()
		{
			_typeToCollection ??= new Dictionary<string, HashSet<ScriptableObject>>();
			var typeName = typeof(T).Name;
			if (!_typeToCollection.TryGetValue(typeName, out var cachedCollection))
			{
				cachedCollection = _typeToCollection[typeName] = HashSetPool<ScriptableObject>.Get();

				// Fill
				foreach (var asset in cache.Values)
					if (asset is T)
						cachedCollection.Add(asset);
			}

			foreach (var asset in cachedCollection)
			{
				if (asset is T cast)
					yield return cast;
			}
		}

		public static void Register(ScriptableObject scriptableObject)
		{
			_cache[scriptableObject.ToGuid()] = scriptableObject;

			if (_typeToCollection == null)
				return;
			var typeName = scriptableObject.GetType().Name;
			if (_typeToCollection.ContainsKey(typeName))
				_typeToCollection[typeName].Add(scriptableObject);
		}

		public static void Refresh<T>(IUniqueContentEntrySource<T> source)
		{
			var asset = (ScriptableObject) source;
			Register(asset);
			Register(typeof(T), source);
		}

		public static void RefreshByValueType(Type type) => Refresh(type, true);

		public static bool TryGetSource(Type type, in SerializableGuid guid, out IContentEntrySource source)
		{
			source = null;

			if (EditorContentEntryMap.Contains(type, in guid))
			{
				source = EditorContentEntryMap.Get(type, in guid);
				if (source != null)
					return true;
			}

			Refresh(type);

			if (EditorContentEntryMap.Contains(type, in guid))
			{
				source = EditorContentEntryMap.Get(type, in guid);
				return source != null;
			}

			return false;
		}

		public static bool TryGetSource(Type type, string id, out IContentEntrySource source)
		{
			source = null;

			if (EditorContentEntryMap.Contains(type, id))
			{
				source = EditorContentEntryMap.Get(type, id);
				if (source != null)
					return true;
			}

			Refresh(type);

			if (EditorContentEntryMap.Contains(type, id))
			{
				source = EditorContentEntryMap.Get(type, id);
				return source != null;
			}

			return false;
		}

		public static bool TryGetSource(Type type, out IContentEntrySource source)
		{
			source = null;

			if (EditorSingleContentEntryShortcut.Contains(type))
			{
				source = EditorSingleContentEntryShortcut.Get(type);
				if (source != null)
					return true;
			}

			Refresh(type);

			if (EditorSingleContentEntryShortcut.Contains(type))
			{
				source = EditorSingleContentEntryShortcut.Get(type);
				return source != null;
			}

			return false;
		}

		public static bool TryGetSource(IContentReference reference, out IContentEntrySource source)
		{
			return TryGetSource(reference, reference.ValueType, out source);
		}

		public static bool TryGetSource(IContentReference reference, Type valueType, out IContentEntrySource source)
		{
			if (reference.IsSingle)
				return TryGetSource(valueType, out source);

			if (reference.IsEmpty())
			{
				source = null;
				return true;
			}

			return TryGetSource(valueType, reference.Guid, out source);
		}

		public static bool AnyByValueType<T>()
		{
			if (EditorSingleContentEntryShortcut<T>.Contains())
				return true;

			return EditorContentEntryMap<T>.Any();
		}

		/// <summary>
		/// Nested не возвращает...
		/// </summary>
		public static IEnumerable<IContentEntrySource<T>> GetAllSourceByValueType<T>()
		{
			Refresh<T>(true);

			if (EditorSingleContentEntryShortcut<T>.Contains())
				yield return EditorSingleContentEntryShortcut<T>.Get();

			foreach (var source in EditorContentEntryMap<T>.GetAll())
				yield return source;
		}

		public static IEnumerable<IContentEntrySource> GetAllSourceByValueType(Type type)
		{
			Refresh(type, true);

			foreach (var source in EditorContentEntryMap.GetAll(type))
				yield return source;
		}

		public static IEnumerable<string> GetAllIdsByValueType(Type type, bool firstEmpty = false)
		{
			if (firstEmpty)
				yield return string.Empty;

			foreach (var source in GetAllSourceByValueType(type))
			{
				if (source.ContentEntry is IIdentifiable identifiable)
					yield return identifiable.Id;
			}
		}

		private static void Refresh<T>(bool force) => Refresh(typeof(T), force);
		private static void Refresh<T>() => Refresh(typeof(T));

		private static void Refresh(Type type, bool force = false)
		{
			if (!force)
				if (_typeToVersion.TryGetValue(type, out var currentVersion)
					&& currentVersion == version)
					return;

			EditorContentEntryMap.Clear(type);
			EditorSingleContentEntryShortcut.Clear(type);

			foreach (var scriptableObject in cache.Values)
			{
				if (scriptableObject is not IContentEntrySource target)
					continue;

				var valueType = target.ContentEntry.ValueType;

				if (valueType == type)
				{
					Register(valueType, target);
					continue;
				}

				if (target.ContentEntry.Nested.IsNullOrEmpty())
					continue;

				Register(valueType, target);
			}

			_typeToVersion[type] = version;
		}

		private static void Register(Type valueType, IContentEntrySource target)
		{
			if (!target.Validate())
				return;

			if (target.ContentEntry.IsUnique())
				EditorContentEntryMap.Register(valueType, target);
			else
				EditorSingleContentEntryShortcut.Register(valueType, target);
		}

		public static event Action<IUniqueContentEntry, string, string> RegeneratedGuid;

		public static void RegenerateGuid(IUniqueContentEntry entry, string path, UnityObject context = null)
		{
			var prevGuid = entry.Guid;
			entry.RegenerateGuid();
			RegeneratedGuid?.Invoke(entry, prevGuid, entry.Guid);

			if (ContentDebug.Logging.Nested.regenerate)
			{
				var msg = $"<b>Regenerated</b> guid [ {entry.Guid}]";
				if (prevGuid != SerializableGuid.Empty)
					msg += $" from [ {prevGuid} ]";
				msg += " for content entry by path: <u>" + path + "</u>";
				ContentDebug.LogWarning(msg, context);
			}
		}
	}

	internal delegate IContentEntrySource EditorTypeSingleResolver();

	internal static class EditorSingleContentEntryShortcut
	{
		private static Dictionary<Type, MethodInfo> _typeToMethod = new(1);
		internal static readonly Dictionary<Type, EditorTypeSingleResolver> typeToResolver = new(1);
		internal static readonly Dictionary<Type, Action> typeToClearAction = new(1);

		static EditorSingleContentEntryShortcut() => ContentEditorCache.Cleared += HandleCleared;

		private static void HandleCleared()
		{
			_typeToMethod.Clear();
			typeToResolver.Clear();
			typeToClearAction.Clear();
		}

		public static bool Contains(Type type) =>
			typeToResolver.TryGetValue(type, out var resolver) && resolver() != null;

		public static IContentEntrySource Get(Type type)
			=> typeToResolver.TryGetValue(type, out var resolver) ? resolver() : null;

		public static void Register(Type type, IContentEntrySource target)
		{
			if (!_typeToMethod.TryGetValue(type, out var methodInfo))
			{
				methodInfo = typeof(EditorSingleContentEntryShortcut<>)
					.MakeGenericType(type)
					// EditorSingleContentEntryShortcut<object> - object выбран первый попавшийся тип
					.GetMethod(nameof(EditorSingleContentEntryShortcut<object>.RegisterRaw),
						BindingFlags.NonPublic | BindingFlags.Static);
				_typeToMethod[type] = methodInfo;
			}

			methodInfo?.Invoke(null, new object[] {target});
		}

		public static void Clear(Type type)
		{
			if (typeToClearAction.TryGetValue(type, out var action))
				action?.Invoke();
		}
	}

	internal static class EditorSingleContentEntryShortcut<T>
	{
		private static IContentEntrySource<T> _source;

		static EditorSingleContentEntryShortcut() => ContentEditorCache.Cleared += HandleCleared;

		private static void HandleCleared()
		{
			_source = null;
		}

		internal static void RegisterRaw(IContentEntrySource raw)
		{
			if (raw is IContentEntrySource<T> source)
				Register(source);
		}

		internal static void Register(IContentEntrySource<T> source)
		{
			if (!EditorSingleContentEntryShortcut.typeToResolver.ContainsKey(typeof(T)))
				EditorSingleContentEntryShortcut.typeToResolver[typeof(T)] = Resolve;
			if (!EditorSingleContentEntryShortcut.typeToClearAction.ContainsKey(typeof(T)))
				EditorSingleContentEntryShortcut.typeToClearAction[typeof(T)] = Clear;

			if (source?.ContentEntry == null)
			{
				ContentDebug.LogWarning("Source is null", (UnityObject) source);
				return;
			}

			if (source.ContentEntry.IsUnique())
			{
				ContentDebug.LogWarning("Source is unique?", (UnityObject) source);
				return;
			}

			//if (Contains())
			{
				// TODO: разобраться позже почему некоторые типы перерегистрируются
				// ContentDebug.LogWarning($"Already registered single entry of type: [ {typeof(T).Name} ]", (UnityObject) source);
			}

			_source = source;

			EditorContentEntryMap.RegisterNestedSafe(source);
		}

		private static IContentEntrySource Resolve() => _source;
		private static void Clear() => _source = null;

		public static IContentEntrySource<T> Get() => _source;

		public static bool Contains() => _source != null;
	}

	internal delegate IContentEntrySource EditorTypeResolver(in SerializableGuid guid, string id = null);

	internal static class EditorContentEntryMap
	{
		private static Dictionary<Type, MethodInfo> _typeToMethod = new(1);
		internal static readonly Dictionary<Type, EditorTypeResolver> typeToResolver = new(1);
		internal static readonly Dictionary<Type, Action> typeToClearAction = new(1);
		internal static readonly Dictionary<Type, Func<bool>> typeToAnyFunc = new(1);
		internal static readonly Dictionary<Type, Func<IEnumerable<IContentEntrySource>>> typeToGetAllFunc = new(1);

		internal static Dictionary<SerializableGuid, NestedContentEntrySource> nestedToSource;

		static EditorContentEntryMap() => ContentEditorCache.Cleared += HandleCleared;

		private static void HandleCleared()
		{
			_typeToMethod.Clear();
			typeToResolver.Clear();
			typeToClearAction.Clear();
			typeToAnyFunc.Clear();
			typeToGetAllFunc.Clear();
			nestedToSource?.Clear();
		}

		public static IEnumerable<IContentEntrySource> GetAll(Type type)
		{
			if (typeToGetAllFunc.TryGetValue(type, out var func))
			{
				foreach (var source in func.Invoke())
					yield return source;
			}
		}

		public static bool Any(Type type)
		{
			if (typeToAnyFunc.TryGetValue(type, out var func))
				return func.Invoke();

			return false;
		}

		public static bool Contains(Type type, in SerializableGuid guid)
		{
			if (typeToResolver.TryGetValue(type, out var resolver))
				return resolver(in guid) != null;

			if (nestedToSource != null && nestedToSource.ContainsKey(guid))
				return true;

			return false;
		}

		public static IContentEntrySource Get(Type type, in SerializableGuid guid)
		{
			if (typeToResolver.TryGetValue(type, out var resolver))
			{
				var resolvedSource = resolver(in guid);
				if (resolvedSource != null)
					return resolvedSource;
			}

			if (nestedToSource != null && nestedToSource.TryGetValue(guid, out var source))
				return source;

			return null;
		}

		public static bool Contains(Type type, string id) =>
			typeToResolver.TryGetValue(type, out var resolver) && resolver(in SerializableGuid.Empty, id) != null;

		public static IContentEntrySource Get(Type type, string id)
		{
			if (typeToResolver.TryGetValue(type, out var resolver))
				return resolver(in SerializableGuid.Empty, id);

			return null;
		}

		public static void Register(Type type, IContentEntrySource target)
		{
			if (!_typeToMethod.TryGetValue(type, out var methodInfo))
			{
				methodInfo = typeof(EditorContentEntryMap<>)
					.MakeGenericType(type)
					.GetMethod("RegisterRaw", BindingFlags.NonPublic | BindingFlags.Static);
				_typeToMethod[type] = methodInfo;
			}

			methodInfo?.Invoke(null, new object[] {target});
		}

		internal static void Clear(Type type)
		{
			if (typeToClearAction.TryGetValue(type, out var action))
				action?.Invoke();
		}

		internal static void RegisterNestedSafe<T>(IContentEntrySource<T> source)
		{
			if (source.ContentEntry.Nested.IsNullOrEmpty())
				return;

			nestedToSource ??= new();
			foreach (var guid in source.ContentEntry.Nested.Keys)
			{
				if (nestedToSource.ContainsKey(guid))
					continue;

				nestedToSource[guid] = new NestedContentEntrySource
				{
					source = source,
					guid   = guid
				};
			}
		}
	}

	internal static class EditorContentEntryMap<T>
	{
		private static readonly Dictionary<SerializableGuid, IUniqueContentEntrySource<T>> _dictionary = new(1);
		private static readonly Dictionary<string, Reference<SerializableGuid>> _idToGuid = new(1);
		private static readonly Dictionary<SerializableGuid, string> _guidToId = new(1);

		static EditorContentEntryMap() => ContentEditorCache.Cleared += HandleCleared;

		private static void HandleCleared()
		{
			_dictionary.Clear();
			_idToGuid.Clear();
			_guidToId.Clear();
		}

		/// <summary>
		/// <see cref="EditorContentEntryMap.Register"/>
		/// </summary>
		internal static void RegisterRaw(IContentEntrySource raw)
		{
			if (raw is IContentEntrySource<T> source)
				Register(source);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void Register(IContentEntrySource<T> source)
		{
			if (!EditorContentEntryMap.typeToResolver.ContainsKey(typeof(T)))
				EditorContentEntryMap.typeToResolver[typeof(T)] = Resolve;
			if (!EditorContentEntryMap.typeToClearAction.ContainsKey(typeof(T)))
				EditorContentEntryMap.typeToClearAction[typeof(T)] = Clear;
			if (!EditorContentEntryMap.typeToAnyFunc.ContainsKey(typeof(T)))
				EditorContentEntryMap.typeToAnyFunc[typeof(T)] = Any;
			if (!EditorContentEntryMap.typeToGetAllFunc.ContainsKey(typeof(T)))
				EditorContentEntryMap.typeToGetAllFunc[typeof(T)] = GetAll;

			if (source is IUniqueContentEntrySource<T> uniqueSource)
			{
				ref readonly var guid = ref uniqueSource.UniqueContentEntry.Guid;
				var id = uniqueSource.Id;

				RemovePreviousIdIfNeeded(in guid, id);
				_dictionary[guid] = uniqueSource;
				_guidToId[guid] = id;
				if (ShouldReplaceIdOwner(id, in guid, uniqueSource))
					_idToGuid[id] = new(guid);
			}

			EditorContentEntryMap.RegisterNestedSafe(source);
		}

		private static void RemovePreviousIdIfNeeded(in SerializableGuid guid, string id)
		{
			if (!_guidToId.TryGetValue(guid, out var previousId) || previousId == id)
				return;

			if (previousId == null || !_idToGuid.TryGetValue(previousId, out var previousGuidReference))
				return;

			if (previousGuidReference.value == guid)
				_idToGuid.Remove(previousId);
		}

		private static bool ShouldReplaceIdOwner(string id, in SerializableGuid guid, IUniqueContentEntrySource<T> source)
		{
			if (!_idToGuid.TryGetValue(id, out var currentGuidReference))
				return true;

			if (currentGuidReference.value == guid)
				return true;

			if (!_dictionary.TryGetValue(currentGuidReference.value, out var currentSource))
				return true;

			return HasIdPriorityOver(source, currentSource);
		}

		private static bool HasIdPriorityOver(IUniqueContentEntrySource<T> source, IUniqueContentEntrySource<T> currentSource)
		{
			if (source.CreationOrder != currentSource.CreationOrder)
				return source.CreationOrder < currentSource.CreationOrder;

			return string.CompareOrdinal(
				source.UniqueContentEntry.Guid.ToString(),
				currentSource.UniqueContentEntry.Guid.ToString()) < 0;
		}

		private static IContentEntrySource Resolve(in SerializableGuid guid, string id = null)
		{
			if (id != null)
			{
				if (!_idToGuid.TryGetValue(id, out var reference))
					return null;

				return _dictionary.GetValueOrDefault(reference.value);
			}

			return _dictionary.GetValueOrDefault(guid);
		}

		public static void Clear()
		{
			_dictionary.Clear();
			_idToGuid.Clear();
			_guidToId.Clear();
		}

		public static bool Contains() => Any();
		public static bool Contains(string id) => _idToGuid.ContainsKey(id);
		public static bool Contains(in SerializableGuid guid) => _dictionary.ContainsKey(guid);

		public static IUniqueContentEntrySource<T> Get(in SerializableGuid guid) => _dictionary[guid];
		public static IUniqueContentEntrySource<T> Get(string id) => _dictionary[_idToGuid[id].value];

		public static string ToId(in SerializableGuid guid) => _dictionary[guid].Id;
		public static ref readonly SerializableGuid ToGuid(string id) => ref _idToGuid[id].value;

		public static IEnumerable<IUniqueContentEntrySource<T>> GetAll() => _dictionary.Values;

		public static bool Any() => !_dictionary.IsEmpty();

		private sealed class Reference<TValue>
			where TValue : struct
		{
			public readonly TValue value;

			public Reference(in TValue value) => this.value = value;

			public override string ToString() => value.ToString();
		}
	}

	internal class NestedContentEntrySource : INestedContentEntrySource
	{
		public IContentEntrySource source;
		public SerializableGuid guid;
		public IContentEntrySource Source => source;

		public IUniqueContentEntry UniqueContentEntry
		{
			get
			{
				var uniqueContentEntry = source.ContentEntry.Nested[guid]
					.Resolve(source.ContentEntry, true);
				uniqueContentEntry?.SetParent(source.ContentEntry);
				return uniqueContentEntry;
			}
		}

		public bool Validate() => source.Validate();
	}
}
