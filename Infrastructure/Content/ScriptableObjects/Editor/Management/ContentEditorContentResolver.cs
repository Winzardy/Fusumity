using System;
using System.Collections.Generic;
using Content.Management;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	[InitializeOnLoad]
	public static class ContentEditorContentResolverAutoSetup
	{
		private static readonly ContentEditorContentResolver _resolver = new();

		static ContentEditorContentResolverAutoSetup() => SetEditorResolver();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void SetEditorResolver() => ContentManager.editorResolver = _resolver;
	}

	public class ContentEditorContentResolver : IContentEditorResolver
	{
		public bool Any<T>() => ContentEditorCache.AnyByValueType<T>();

		public UniqueContentEntry<T> GetEntry<T>(in SerializableGuid guid)
		{
			if (ContentEditorCache.TryGetSource(typeof(T), in guid, out var source) &&
			    source.ContentEntry is UniqueContentEntry<T> contentEntry)
				return contentEntry;

			throw ContentDebug.NullException($"Could not find unique value of type [ {typeof(T).Name} ] by guid [ {guid} ] ");
		}

		public UniqueContentEntry<T> GetEntry<T>(string id)
		{
			if (ContentEditorCache.TryGetSource(typeof(T), id, out var source) &&
			    source.ContentEntry is UniqueContentEntry<T> contentEntry)
				return contentEntry;

			throw ContentDebug.NullException($"Could not find value of type [ {typeof(T).Name} ] by id [ {id} ]");
		}

		public UniqueContentEntry<T> GetEntry<T>(int index) => throw new NotImplementedException("Index can only be used at runtime");

		public SingleContentEntry<T> GetEntry<T>()
		{
			if (ContentEditorCache.TryGetSource(typeof(T), out var source) &&
			    source.ContentEntry is SingleContentEntry<T> singleContentEntry)
				return singleContentEntry;

			throw ContentDebug.NullException($"Could not find single value of type [ {typeof(T).Name} ]");
		}

		public ref readonly T Get<T>(in SerializableGuid guid) => ref GetEntry<T>(in guid).Value;

		public ref readonly T Get<T>(string id) => ref GetEntry<T>(id).Value;

		public ref readonly T Get<T>(int index) => throw new NotImplementedException("Index can only be used at runtime");

		public ref readonly T Get<T>() => ref GetEntry<T>().Value;

		public bool Contains<T>(in SerializableGuid guid) => ContentEditorCache.TryGetSource(typeof(T), in guid, out _);

		public bool Contains<T>(string id) => ContentEditorCache.TryGetSource(typeof(T), id, out _);

		public bool Contains<T>(int index) => throw new NotImplementedException("Index can only be used at runtime");

		public bool Contains<T>() => ContentEditorCache.TryGetSource(typeof(T), out _);

		public IEnumerable<IContentEntry<T>> GetAll<T>() => ContentEditorCache.GetAllSourceByValueType<T>();

		public string ToId<T>(in SerializableGuid guid)
		{
			if (ContentEditorCache.TryGetSource(typeof(T), in guid, out var source) &&
			    source.ContentEntry is UniqueContentEntry<T> contentEntry)
				return contentEntry.Id;

			return guid.ToString();
		}

		public string ToId<T>(int index) => throw new NotImplementedException("Index can only be used at runtime");

		public ref readonly SerializableGuid ToGuid<T>(string id)
		{
			try
			{
				if (ContentEditorCache.TryGetSource(typeof(T), id, out var source) &&
				    source.ContentEntry is UniqueContentEntry<T> contentEntry)
					return ref contentEntry.Guid;
			}
			catch (Exception e)
			{
				ContentDebug.LogWarning(e.Message);
			}

			return ref SerializableGuid.Empty;
		}
		public string ToLabel<T>(in SerializableGuid guid, bool detailed = false)
		{
			if (guid == IContentReference.SINGLE_GUID)
			{
				if (Contains<T>())
				{
					var entry = GetEntry<T>();
					return detailed
						? $"{IContentEntry.DEFAULT_SINGLE_ID} (type: {entry.ValueType.Name})"
						: $"{IContentEntry.DEFAULT_SINGLE_ID}";
				}
			}
			else if (Contains<T>(in guid))
			{
				var entry = GetEntry<T>(in guid);
				return detailed
					? $"{entry.Id} (type:{entry.ValueType.Name}, guid: {guid})"
					: $"{entry.Id}";
			}

			return $"[{typeof(T).Name}] {guid}";
		}

		public ref readonly SerializableGuid ToGuid<T>(int index) =>
			throw new NotImplementedException("Index can only be used at runtime");

		public int ToIndex<T>(in SerializableGuid guid) =>
			throw new NotImplementedException("Index can only be used at runtime");

		public int ToIndex<T>(string id) => throw new NotImplementedException("Index can only be used at runtime");

	}
}
