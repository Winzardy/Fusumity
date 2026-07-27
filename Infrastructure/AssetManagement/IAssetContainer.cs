using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Collections;
using Sapientia.Pooling;
using Object = UnityEngine.Object;

namespace AssetManagement
{
	public interface IAssetContainerState
	{
		object Key { get; }
		object Asset { get; }
		bool IsLoaded { get; }
		int UsageCount { get; }
		float Progress { get; }

		/// <summary>
		/// Сколько секунд осталось до отложенной выгрузки, либо null если выгрузка не запланирована
		/// </summary>
		double? ReleaseRemainingSeconds => null;
	}

	public interface IAssetContainer : IProgress<float>
	{
		IAssetContainerState State { get; }

		/// <summary>
		/// Загружает или ожидает загрузку ассета из уже полученного контейнера,
		/// не увеличивая <see cref="IAssetContainerState.UsageCount"/>
		/// </summary>
		UniTask<T> LoadAsync<T>(CancellationToken cancellationToken = default, IProgress<float> progress = null);

		/// <summary>
		/// Освобождает один usage контейнера
		/// При <paramref name="full"/> принудительно освобождает контейнер независимо от количества usages
		/// </summary>
		void Release(bool full = false);
	}

	internal struct AssetProgressRelay
	{
		private float _value;
		private ProgressTarget _single;
		private List<ProgressTarget> _additional;

		public float Value { get => _value; }

		public void Attach(IProgress<float> progress)
		{
			if (progress == null)
				return;

			progress.Report(_value);

			if (_single.progress == null)
			{
				_single = new ProgressTarget(progress);
				return;
			}

			if (ReferenceEquals(_single.progress, progress))
			{
				_single.usages++;
				return;
			}

			if (_additional != null)
			{
				for (var i = 0; i < _additional.Count; i++)
				{
					var target = _additional[i];
					if (!ReferenceEquals(target.progress, progress))
						continue;

					target.usages++;
					_additional[i] = target;
					return;
				}
			}

			_additional ??= ListPool<ProgressTarget>.Get();
			_additional.Add(new ProgressTarget(progress));
		}

		public void Detach(IProgress<float> progress)
		{
			if (progress == null)
				return;

			if (ReferenceEquals(_single.progress, progress))
			{
				if (--_single.usages > 0)
					return;

				_single = default;
				PromoteSingle();
				return;
			}

			if (_additional == null)
				return;

			for (var i = 0; i < _additional.Count; i++)
			{
				var target = _additional[i];
				if (!ReferenceEquals(target.progress, progress))
					continue;

				if (--target.usages > 0)
				{
					_additional[i] = target;
					return;
				}

				_additional.RemoveAt(i);
				if (_additional.Count == 0)
					StaticObjectPoolUtility.ReleaseAndSetNull(ref _additional);
				return;
			}
		}

		public void Report(float value)
		{
			value = value < 0f ? 0f : value > 1f ? 1f : value;
			if (_value.Equals(value))
				return;

			_value = value;
			_single.progress?.Report(value);

			if (_additional == null)
				return;

			for (var i = 0; i < _additional.Count; i++)
				_additional[i].progress.Report(value);
		}

		public void Dispose()
		{
			_value = 0f;
			_single = default;

			if (_additional != null)
				StaticObjectPoolUtility.ReleaseAndSetNull(ref _additional);
		}

		private void PromoteSingle()
		{
			if (_additional == null || _additional.Count == 0)
				return;

			var lastIndex = _additional.Count - 1;
			_single = _additional[lastIndex];
			_additional.RemoveAt(lastIndex);

			if (_additional.Count == 0)
				StaticObjectPoolUtility.ReleaseAndSetNull(ref _additional);
		}

		private struct ProgressTarget
		{
			public readonly IProgress<float> progress;
			public int usages;

			public ProgressTarget(IProgress<float> progress)
			{
				this.progress = progress;
				usages = 1;
			}
		}
	}

	public sealed class AssetContainerMediator<T> : IDisposable
		where T : Object
	{
		private Entry _single;
		private HashMap<object, Entry> _additional;
		private bool _disposed;

		public async UniTask<T> LoadAsync(IAssetReference reference, CancellationToken cancellationToken = default,
			IProgress<float> progress = null)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var entry = GetOrCreate(reference);

			if (!cancellationToken.CanBeCanceled)
				return await LoadAsync(entry, entry.cancellation.Token, progress);

			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
				cancellationToken,
				entry.cancellation.Token);
			return await LoadAsync(entry, linkedCts.Token, progress);
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
			key = container.State.Key;

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

		private static async UniTask<T> LoadAsync(Entry entry, CancellationToken cancellationToken,
			IProgress<float> progress)
		{
			cancellationToken.ThrowIfCancellationRequested();

			T asset;
			var state = entry.container.State;
			if (state.IsLoaded)
			{
				asset = (T) state.Asset;
				progress?.Report(1f);
			}
			else
			{
				asset = await entry.container.LoadAsync<T>(cancellationToken, progress);
			}

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
