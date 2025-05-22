using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fusumity.Editor.Utility;
using Sapientia;
using Sapientia.Collections;
using UnityEngine;

namespace Content.Editor
{
	public static class ContentEditorCache
	{
		private static Dictionary<string, ScriptableObject> _cache;

		private static Dictionary<string, ScriptableObject> cache
		{
			get
			{
				if (_cache == null)
					Refresh();

				return _cache;
			}
		}

		public static ScriptableObject GetAsset(string guid) => cache[guid];

		public static void Refresh()
		{
			_cache ??= new();
			_cache.Clear();
			foreach (var scriptableObject in AssetDatabaseUtility.GetAssets<ScriptableObject>())
				cache[scriptableObject.ToGuid()] = scriptableObject;
		}

		public static bool TryGetSource(Type type, in SerializableGuid guid, out IContentEntrySource source)
		{
			source = null;

			if (EditorContentEntryMap.Contains(type, in guid))
			{
				source = EditorContentEntryMap.Get(type, in guid);
				return true;
			}

			Refresh(type);

			if (EditorContentEntryMap.Contains(type, in guid))
			{
				source = EditorContentEntryMap.Get(type, in guid);
				return true;
			}

			return false;
		}

		public static bool TryGetSource(Type type, string id, out IContentEntrySource source)
		{
			source = null;

			if (EditorContentEntryMap.Contains(type, id))
			{
				source = EditorContentEntryMap.Get(type, id);
				return true;
			}

			Refresh(type);

			if (EditorContentEntryMap.Contains(type, id))
			{
				source = EditorContentEntryMap.Get(type, id);
				return true;
			}

			return false;
		}

		public static bool TryGetSource(Type type, out IContentEntrySource source)
		{
			source = null;

			if (EditorSingleContentEntryShortcut.Contains(type))
			{
				source = EditorSingleContentEntryShortcut.Get(type);
				return true;
			}

			Refresh(type);

			if (EditorSingleContentEntryShortcut.Contains(type))
			{
				source = EditorSingleContentEntryShortcut.Get(type);
				return true;
			}

			return false;
		}

		public static bool TryGetSource(IContentReference reference, out IContentEntrySource source)
		{
			if (reference.IsSingle)
				return TryGetSource(reference.ValueType, out source);

			if (reference.IsEmpty())
			{
				source = null;
				return false;
			}

			return TryGetSource(reference.ValueType, reference.Guid, out source);
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
		public static IEnumerable<IContentEntry<T>> GetAllSourceByValueType<T>()
		{
			if (EditorSingleContentEntryShortcut<T>.Contains())
				yield return EditorSingleContentEntryShortcut<T>.Get().ContentEntry;

			foreach (var entry in EditorContentEntryMap<T>.GetAll())
				yield return entry.ContentEntry;
		}

		private static void Refresh<T>() => Refresh(typeof(T));

		private static void Refresh(Type type)
		{
			EditorContentEntryMap.Clear(type);
			foreach (var scriptableObject in cache.Values)
			{
				if (scriptableObject is IContentEntrySource target)
				{
					var valueType = target.ContentEntry.ValueType;
					if (valueType == type)
					{
						EditorContentEntryMap.Register(valueType, target);

						if (!target.ContentEntry.IsUnique())
							EditorSingleContentEntryShortcut.Register(valueType, target);
					}

					if (target.ContentEntry.Nested.IsNullOrEmpty())
						continue;

					EditorContentEntryMap.Register(valueType, target);

					if (!target.ContentEntry.IsUnique())
						EditorSingleContentEntryShortcut.Register(valueType, target);
				}
			}
		}
	}

	internal delegate IContentEntrySource EditorTypeSingleResolver();

	internal static class EditorSingleContentEntryShortcut
	{
		private static Dictionary<Type, MethodInfo> _typeToMethod = new(1);
		internal static Dictionary<Type, EditorTypeSingleResolver> _dictionary = new(1);

		public static bool Contains(Type type) =>
			_dictionary.TryGetValue(type, out var resolver) && resolver() != null;

		public static IContentEntrySource Get(Type type)
		{
			if (_dictionary.TryGetValue(type, out var resolver))
				return resolver();
			return null;
		}

		public static void Register(Type type, IContentEntrySource target)
		{
			if (!_typeToMethod.TryGetValue(type, out var methodInfo))
			{
				methodInfo = typeof(EditorSingleContentEntryShortcut<>)
				   .MakeGenericType(type)
				   .GetMethod("RegisterRaw", BindingFlags.NonPublic | BindingFlags.Static);
				_typeToMethod[type] = methodInfo;
			}

			methodInfo?.Invoke(null, new object[] {target});
		}
	}

	internal static class EditorSingleContentEntryShortcut<T>
	{
		private static IContentEntrySource<T> _source;

		internal static void RegisterRaw(IContentEntrySource raw)
		{
			if (raw is IContentEntrySource<T> source)
				Register(source);
		}

		internal static void Register(IContentEntrySource<T> entry)
		{
			if (!EditorSingleContentEntryShortcut._dictionary.ContainsKey(typeof(T)))
				EditorSingleContentEntryShortcut._dictionary[typeof(T)] = Resolve;

			if (Contains())
				ContentDebug.LogError($"Already registered single entry of type: [ {typeof(T).Name} ]");

			_source = entry;
		}

		private static IContentEntrySource Resolve() => _source;

		public static IContentEntrySource<T> Get() => _source;

		public static bool Contains() => _source != null;
	}

	internal delegate IContentEntrySource EditorTypeResolver(in SerializableGuid guid, string id = null);

	internal static class EditorContentEntryMap
	{
		private static Dictionary<Type, MethodInfo> _typeToMethod = new(1);
		internal static readonly Dictionary<Type, EditorTypeResolver> typeToResolver = new(1);
		internal static readonly Dictionary<Type, Action> typeToClearAction = new(1);

		internal static Dictionary<SerializableGuid, NestedContentEntrySource> nestedToSource;

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
	}

	internal static class EditorContentEntryMap<T>
	{
		private static readonly Dictionary<SerializableGuid, IUniqueContentEntrySource<T>> _dictionary = new(1);
		private static readonly Dictionary<string, Reference<SerializableGuid>> _idToGuid = new(1);

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

			if (source is IUniqueContentEntrySource<T> uniqueSource)
			{
				ref readonly var guid = ref uniqueSource.UniqueContentEntry.Guid;
				var id = uniqueSource.Id;

				_dictionary[guid] = uniqueSource;
				_idToGuid[id] = new(guid);
			}

			if (source.ContentEntry.Nested.IsNullOrEmpty())
				return;

			EditorContentEntryMap.nestedToSource ??= new();
			foreach (var guid in source.ContentEntry.Nested.Keys)
			{
				if (EditorContentEntryMap.nestedToSource.ContainsKey(guid))
					continue;

				EditorContentEntryMap.nestedToSource[guid] = new NestedContentEntrySource
				{
					source = source,
					guid = guid
				};
			}
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

	public static class ContentReferenceExtensions
	{
		public static string ToLabel<T>(this ContentReference<T> reference)
		{
			if (ContentEditorCache.TryGetSource(typeof(T), in reference.guid, out var source))
			{
				if (source.ContentEntry is IIdentifiable identifiable)
					return identifiable.Id;
			}

			return reference.guid.ToString();
		}
	}

	internal class NestedContentEntrySource : INestedContentEntrySource
	{
		public IContentEntrySource source;
		public SerializableGuid guid;
		public IContentEntrySource Source => source;

		public IUniqueContentEntry UniqueContentEntry
			=> source.ContentEntry.Nested[guid]
			   .Resolve(source.ContentEntry, true);
	}
}
