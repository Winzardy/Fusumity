using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Management;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	[InitializeOnLoad]
	public static class ContentEditorContentResolverAutoSetup
	{
		private static readonly ContentEditorResolver _resolver = new();

		static ContentEditorContentResolverAutoSetup()
		{
			SetEditorResolver();
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void SetEditorResolver()
		{
			ContentManager.editorResolver = _resolver;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

			if (state == PlayModeStateChange.ExitingPlayMode)
				ContentManager.Terminate();
		}
	}

	public class ContentEditorResolver : IContentEditorResolver
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

		public bool TryGetEntry<T>(in SerializableGuid guid, out UniqueContentEntry<T> entry)
		{
			entry = null;
			if (ContentEditorCache.TryGetSource(typeof(T), in guid, out var source) &&
				source.ContentEntry is UniqueContentEntry<T> contentEntry)
			{
				entry = contentEntry;
				return true;
			}

			return false;
		}

		public bool TryGetEntry<T>(string id, out UniqueContentEntry<T> entry)
		{
			entry = null;
			if (ContentEditorCache.TryGetSource(typeof(T), id, out var source) &&
				source.ContentEntry is UniqueContentEntry<T> contentEntry)
			{
				entry = contentEntry;
				return true;
			}

			return false;
		}

		public bool TryGetEntry<T>(int index, out UniqueContentEntry<T> entry)
		{
			entry = null;
			return false;
		}

		public bool TryGetEntry<T>(out SingleContentEntry<T> entry)
		{
			entry = null;
			if (ContentEditorCache.TryGetSource(typeof(T), out var source) &&
				source.ContentEntry is SingleContentEntry<T> singleContentEntry)
			{
				entry = singleContentEntry;
				return true;
			}

			return false;
		}

		public ref readonly T Get<T>(in SerializableGuid guid) => ref GetEntry<T>(in guid).Value;

		public ref readonly T Get<T>(string id) => ref GetEntry<T>(id).Value;

		public ref readonly T Get<T>(int index) => throw new NotImplementedException("Index can only be used at runtime");

		public ref readonly T Get<T>() => ref GetEntry<T>().Value;

		public bool Contains<T>(in SerializableGuid guid) => ContentEditorCache.TryGetSource(typeof(T), in guid, out _);

		public bool Contains<T>(string id) => ContentEditorCache.TryGetSource(typeof(T), id, out _);

		public bool Contains<T>(int index) => throw new NotImplementedException("Index can only be used at runtime");

		public bool Contains<T>() => ContentEditorCache.TryGetSource(typeof(T), out _);

		public IEnumerable<IContentEntry<T>> GetAllEntries<T>() => ContentEditorCache.GetAllSourceByValueType<T>();

		public IEnumerable<ContentReference<T>> GetAll<T>()
		{
			foreach (var entry in GetAllEntries<T>())
				yield return entry.ToReference();
		}

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

			ContentDebug.LogError($"Could not find entry of type: [ {typeof(T).Name} ] with id: [ {id} ]");
			return ref SerializableGuid.Empty;
		}

		/// <see cref="ContentResolver.ToLabel{T}(in SerializableGuid, bool)"/>
		public string ToLabel<T>(in SerializableGuid guid, bool verbose = false)
		{
			if (guid == IContentReference.SINGLE_GUID)
			{
				if (Contains<T>())
				{
					var entry = GetEntry<T>();
					return verbose
						? $"{ContentConstants.DEFAULT_SINGLE_ID} (type: {entry.ValueType.Name})"
						: $"{ContentConstants.DEFAULT_SINGLE_ID}";
				}
			}
			else if (Contains<T>(in guid))
			{
				var entry = GetEntry<T>(in guid);
				return verbose
					? $"{entry.Id} (type:{entry.ValueType.Name}, guid: {guid})"
					: $"{entry.Id}";
			}

			return verbose
				? $"[{typeof(T).Name}] {guid}"
				: guid.ToString();
		}

		public Task PopulateAsync(IContentImporter importer, CancellationToken token = default)
			=> Task.CompletedTask;

		public bool IsFullyLoaded() => true;

		public ref readonly SerializableGuid ToGuid<T>(int index) =>
			throw new NotImplementedException("Index can only be used at runtime");

		public int ToIndex<T>(in SerializableGuid guid) =>
			throw new NotImplementedException("Index can only be used at runtime");

		public int ToIndex<T>(string id) => throw new NotImplementedException("Index can only be used at runtime");
	}
}
