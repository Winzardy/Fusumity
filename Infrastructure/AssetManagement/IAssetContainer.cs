using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Collections;
using Sapientia.Pooling;
using Object = UnityEngine.Object;

namespace AssetManagement
{
	public interface IAssetContainer
	{
		object Key { get; }
		object Asset { get; }
		bool IsLoaded { get; }
		int ReferenceCount { get; }

		UniTask<T> LoadAsync<T>(CancellationToken cancellationToken = default, IProgress<float> progress = null);
		void Release();
	}

	public sealed class AssetContainerMediator<T> : IDisposable
		where T : Object
	{
		private Entry _single;
		private HashMap<object, Entry> _additional;
		private bool _disposed;

		public async UniTask<T> LoadAsync(IAssetReference reference, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var entry = GetOrCreate(reference);

			if (!cancellationToken.CanBeCanceled)
				return await LoadAsync(entry, entry.cancellation.Token);

			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
				cancellationToken,
				entry.cancellation.Token);
			return await LoadAsync(entry, linkedCts.Token);
		}

		public void Release(IAssetReference reference)
		{
			if (_disposed || !TryRemove(reference?.RuntimeKey, out var entry))
				return;

			Release(entry);
		}

		public void Clear()
		{
			if (_single.container != null)
			{
				Release(_single);
				_single = default;
			}

			if (_additional == null)
				return;

			foreach (var entry in _additional)
				Release(entry);

			StaticObjectPoolUtility.ReleaseAndSetNull(ref _additional);
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;
			Clear();
		}

		private Entry GetOrCreate(IAssetReference reference)
		{
			if (_disposed)
				throw new ObjectDisposedException(nameof(AssetContainerMediator<T>));

			var key = reference?.RuntimeKey;
			if (TryGet(key, out var entry))
				return entry;

			var container = AssetLoader.AcquireAssetContainer<T>(reference);
			key = container.Key;

			// RuntimeKey должен быть стабильным, но повторно проверяем канонический ключ контейнера
			if (TryGet(key, out entry))
			{
				container.Release();
				return entry;
			}

			if (key == null)
			{
				container.Release();
				throw AssetManagementDebug.Exception("Asset container key must not be null");
			}

			entry = new Entry(key, container, new CancellationTokenSource());
			Add(entry);
			return entry;
		}

		private static async UniTask<T> LoadAsync(Entry entry, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var asset = entry.container.IsLoaded
				? (T) entry.container.Asset
				: await entry.container.LoadAsync<T>(cancellationToken);

			cancellationToken.ThrowIfCancellationRequested();
			return asset;
		}

		private bool TryGet(object key, out Entry entry)
		{
			if (key != null && _single.container != null && Equals(_single.key, key))
			{
				entry = _single;
				return true;
			}

			if (key != null && _additional != null && _additional.TryGetValue(key, out entry))
				return true;

			entry = default;
			return false;
		}

		private void Add(Entry entry)
		{
			if (_single.container == null)
			{
				_single = entry;
				return;
			}

			_additional ??= HashMapPool<object, Entry>.Get();
			_additional.SetOrAdd(entry.key, entry);
		}

		private bool TryRemove(object key, out Entry entry)
		{
			if (key != null && _single.container != null && Equals(_single.key, key))
			{
				entry = _single;
				_single = default;
				PromoteSingle();
				return true;
			}

			if (key == null || _additional == null ||
				!_additional.Remove(key, out var removed) || !removed.HasValue)
			{
				entry = default;
				return false;
			}

			entry = removed.Value;

			if (_additional.Count == 0)
				StaticObjectPoolUtility.ReleaseAndSetNull(ref _additional);
			else
				PromoteSingle();

			return true;
		}

		private void PromoteSingle()
		{
			if (_single.container != null || _additional == null || _additional.Count != 1)
				return;

			foreach (var entry in _additional)
			{
				_single = entry;
				break;
			}

			StaticObjectPoolUtility.ReleaseAndSetNull(ref _additional);
		}

		private static void Release(Entry entry)
		{
			entry.cancellation.Cancel();
			entry.cancellation.Dispose();
			entry.container.Release();
		}

		private readonly struct Entry
		{
			public readonly object key;
			public readonly IAssetContainer container;
			public readonly CancellationTokenSource cancellation;

			public Entry(object key, IAssetContainer container, CancellationTokenSource cancellation)
			{
				this.key = key;
				this.container = container;
				this.cancellation = cancellation;
			}
		}
	}
}
