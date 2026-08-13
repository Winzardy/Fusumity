using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AssetManagement.AddressableAssets;
using Cysharp.Threading.Tasks;
using Sapientia.Extensions;
using Sapientia.Pooling;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace AssetManagement
{
	public partial class AssetProvider : IDisposable
	{
		internal const string NULL_KEY_TEXT = "<null>";

		private const int RELEASED_ASSETS_HISTORY_LIMIT = 64;

		private bool _initialized;

		// История отпущенных контейнеров: ассет мог физически остаться в памяти (бандл держат соседние ассеты)
		// WeakReference не удерживает ассет и не мешает выгрузке
		private readonly List<ReleasedAssetEntry> _releasedAssets = new(16);

		public bool IsInitialized { get => _initialized; }

		public void Dispose()
		{
			_initialized = false;

			DisposeAddressable();
			DisposeResources();
		}

		public UniTask InitializeAsync(CancellationToken cancellationToken)
			=> InitializeAsync(null, cancellationToken);

		public async UniTask InitializeAsync(IEnumerable<AssetLabelReference> dependencyLabels,
			CancellationToken cancellationToken)
		{
			await WarmUpAddressables(cancellationToken);

			if (dependencyLabels != null)
			{
				foreach (var label in dependencyLabels)
					await DownloadDependenciesAsync(label, cancellationToken);
			}

			_initialized = true;
			SubscribeDelayedAssetReleases();
		}

		public void CollectAssetContainers(List<IAssetContainer> containers)
		{
			if (containers == null)
				throw new ArgumentNullException(nameof(containers));

			containers.Clear();

			if (_keyToAssetContainer != null)
			{
				foreach (var container in _keyToAssetContainer.Values)
					containers.Add(container);
			}

			if (_keyToResourceContainer == null)
				return;

			foreach (var container in _keyToResourceContainer.Values)
				containers.Add(container);
		}

		public void CollectAssetContainerStates(List<IAssetContainerState> states)
		{
			if (states == null)
				throw new ArgumentNullException(nameof(states));

			states.Clear();

			if (_keyToAssetContainer != null)
			{
				foreach (var container in _keyToAssetContainer.Values)
					states.Add(container);
			}

			if (_keyToAssetCollectionContainer != null)
			{
				foreach (var container in _keyToAssetCollectionContainer.Values)
					states.Add(container);
			}

			if (_keyToResourceContainer == null)
				return;

			foreach (var container in _keyToResourceContainer.Values)
				states.Add(container);
		}

		public string BuildAssetContainersReport()
		{
			using (ListPool<IAssetContainerState>.Get(out var states))
			using (StringBuilderPool.Get(out var builder))
			{
				CollectAssetContainerStates(states);

				var totalUsages = 0;
				var totalMemory = 0L;
				foreach (var state in states)
				{
					totalUsages += state.UsageCount;
					totalMemory += AssetMemoryUtility.GetSize(state.Asset);
				}

				builder.Append("Asset containers report")
				   .Append("\nContainers: ").Append(states.Count)
				   .Append(" | Total usages: ").Append(totalUsages)
				   .Append(" | Total memory: ").Append(AssetMemoryUtility.FormatBytes(totalMemory));

				for (var i = 0; i < states.Count; i++)
				{
					var state = states[i];

					builder.Append("\n\n[").Append(i + 1).Append("]")
					   .Append(" usages=").Append(state.UsageCount)
					   .Append(" loaded=").Append(state.IsLoaded)
					   .Append(" progress=").Append(state.Progress.ToString("P0"));

					// Размер пишем только там, где нативная память показательна (текстуры, спрайты, аудио, меши)
					var memory = AssetMemoryUtility.GetSize(state.Asset);
					if (memory > 0L)
						builder.Append(" memory=").Append(AssetMemoryUtility.FormatBytes(memory));

					switch (state.Asset)
					{
						case UnityObject asset:
							AppendAsset(builder, asset);
							break;
						case IEnumerable assets:
							foreach (var item in assets)
							{
								if (item is UnityObject collectionAsset)
									AppendAsset(builder, collectionAsset);
							}
							break;
						default:
							builder.Append("\nasset: <not loaded>");
							break;
					}

					builder
					   .Append("\npath: ").Append(ResolveAssetPath(state.Key))
					   .Append("\nkey: ").Append(state.Key);

					var bundleName = ResolveBundleName(state.Key);
					if (!bundleName.IsNullOrEmpty())
						builder.Append("\nbundle: ").Append(bundleName);
				}

				AppendReleasedAssets(builder);

				return builder.ToString();
			}
		}

		private static void AppendAsset(System.Text.StringBuilder builder, UnityObject asset)
		{
			builder.Append("\nasset: ").Append(asset != null ? asset.name : "<null>");

#if UNITY_EDITOR
			if (asset == null)
				return;

			var path = UnityEditor.AssetDatabase.GetAssetPath(asset);
			if (path is {Length: > 0})
				builder.Append(" | ").Append(path);
#endif
		}

		internal void TrackReleasedAsset(object key, UnityObject asset)
		{
			if (asset == null || key == null)
				return;

			for (var i = 0; i < _releasedAssets.Count; i++)
			{
				if (!Equals(_releasedAssets[i].key, key))
					continue;

				_releasedAssets[i] = new ReleasedAssetEntry(key, asset);
				return;
			}

			if (_releasedAssets.Count >= RELEASED_ASSETS_HISTORY_LIMIT)
				_releasedAssets.RemoveAt(0);

			_releasedAssets.Add(new ReleasedAssetEntry(key, asset));
		}

		private void AppendReleasedAssets(System.Text.StringBuilder builder)
		{
			if (_releasedAssets.Count == 0)
				return;

			builder.Append("\n\nReleased assets (containers disposed):");

			for (var i = 0; i < _releasedAssets.Count; i++)
			{
				var entry = _releasedAssets[i];

				// Живой натив за WeakReference — ассет всё ещё в памяти, хоть контейнер и отпущен
				var inMemory = entry.assetReference.TryGetTarget(out var asset) && asset != null;

				builder.Append("\n[").Append(i + 1).Append("] ")
				   .Append(entry.assetName);

				if (inMemory)
				{
					builder.Append(" | IN MEMORY");

					var memory = AssetMemoryUtility.GetSize(asset);
					if (memory > 0L)
						builder.Append(' ').Append(AssetMemoryUtility.FormatBytes(memory));

					var bundleName = ResolveBundleName(entry.key);
					builder.Append(bundleName.IsNullOrEmpty()
						? " (bundle not unloaded)"
						: $" ({bundleName} not unloaded)");
				}
				else
					builder.Append(" | unloaded");

				builder.Append(" | key: ").Append(entry.key);
			}
		}

		private readonly struct ReleasedAssetEntry
		{
			public readonly object key;
			public readonly string assetName;
			public readonly WeakReference<UnityObject> assetReference;

			public ReleasedAssetEntry(object key, UnityObject asset)
			{
				this.key = key;
				assetName = asset.name;
				assetReference = new WeakReference<UnityObject>(asset);
			}
		}

		private static void ThrowIfReferenceIsEmpty(IAssetReference reference)
		{
#if UNITY_EDITOR
			if (reference.IsEmptyOrInvalid())
				throw AssetManagementDebug.Exception($"{nameof(reference)} must not be empty");
#endif
		}

		/// <summary>
		/// Загрузить ассет (текстура, геймобж, текст и т.д). <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ассета</typeparam>
		public async UniTask<T> LoadAssetAsync<T>(IAssetReference reference, CancellationToken cancellationToken = default,
			IProgress<float> progress = null)
		{
			ThrowIfReferenceIsEmpty(reference);

			if (reference == null)
			{
				if (typeof(Component).IsAssignableFrom(typeof(T)))
					ThrowInvalidComponentReference<T>();

				ThrowInvalidAssetReference<T>();
			}

			var assetReference = reference.AssetReference;

			if (typeof(Component).IsAssignableFrom(typeof(T)))
				return await LoadComponentAsync<T>(assetReference, cancellationToken, progress);

			return await LoadAssetAsync<T>(assetReference, cancellationToken, progress);
		}

		public IAssetContainer AcquireAssetContainer<T>(IAssetReference reference)
			where T : UnityEngine.Object
		{
			ThrowIfReferenceIsEmpty(reference);

			if (reference == null)
				ThrowInvalidAssetReference<T>();

			if (typeof(Component).IsAssignableFrom(typeof(T)))
				ThrowInvalidComponentReference<T>();

			if (!_initialized)
				throw AssetManagementDebug.OperationCanceledException(default(CancellationToken));

			var assetReference = reference.AssetReference;
			var context = assetReference.GetEditorAssetSafe();
			if (!assetReference.IsRuntimeValid())
				ThrowInvalidAssetReference<T>(context);

			var container = AcquireAssetContainerByKey<T>(reference.RuntimeKey, context);
			container.TryUpdateReleaseDelay(reference.ReleaseDelayMs.Max(0));
			return container;
		}

		/// <summary>
		/// Загрузить GameObject и получить у него выбранный компонент. <br/>
		/// Чтобы подгрузить GameObject используйте <see cref="LoadAssetAsync{T}(IAssetReference,System.Threading.CancellationToken)"/> <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public async UniTask<T> LoadComponentAsync<T>(IAssetReference reference, CancellationToken cancellationToken,
			IProgress<float> progress = null)
			where T : Component
		{
			ThrowIfReferenceIsEmpty(reference);

			if (reference == null)
				ThrowInvalidComponentReference<T>();

			var assetReference = reference.AssetReference;
			return await LoadComponentAsync<T>(assetReference, cancellationToken, progress);
		}

		/// <summary>
		/// Загрузить ассет по пути (текстура, геймобж, текст и т.д).
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(string)"/>
		/// </summary>
		/// <typeparam name="T">Тип ассета</typeparam>
		public async UniTask<T> LoadAssetAsync<T>(string path, CancellationToken cancellationToken,
			IProgress<float> progress = null)
		{
			return await LoadAssetAsyncByKey<T>(path, cancellationToken, progress: progress);
		}

		/// <summary>
		/// Загрузить GameObject и получить у него выбранный компонент. <br/>
		/// Чтобы подгрузить GameObject используйте <see cref="LoadAsync{T}(string,System.Threading.CancellationToken)"/> <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(string)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public async UniTask<T> LoadComponentAsync<T>(string path, CancellationToken cancellationToken,
			IProgress<float> progress = null)
			where T : Component
		{
			return await LoadComponentByKeyAsync<T>(path, cancellationToken, progress: progress);
		}

		/// <summary>
		/// Синхронно загрузить ассет. Блокирует поток до готовности (<see cref="UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion"/>). <br/>
		/// Только для редких кейсов! Вызывает хич на главном потоке и не поддерживается на WebGL <br/>
		/// Ассет обязательно нужно отпустить (release) после использования <see cref="Release(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ассета</typeparam>
		public T LoadAsset<T>(IAssetReference reference)
		{
			ThrowIfReferenceIsEmpty(reference);
			ThrowIfSyncLoadingUnsupported();

			if (reference == null)
			{
				if (typeof(Component).IsAssignableFrom(typeof(T)))
					ThrowInvalidComponentReference<T>();

				ThrowInvalidAssetReference<T>();
			}

			var assetReference = reference.AssetReference;

			if (typeof(Component).IsAssignableFrom(typeof(T)))
				return LoadComponent<T>(assetReference);

			return LoadAsset<T>(assetReference);
		}

		/// <summary>
		/// Синхронно загрузить ассет по пути. См. <see cref="LoadAsset{T}(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ассета</typeparam>
		public T LoadAsset<T>(string path)
		{
			ThrowIfSyncLoadingUnsupported();

			return LoadAssetByKey<T>(path);
		}

		/// <summary>
		/// Синхронно загрузить GameObject и получить у него выбранный компонент. См. <see cref="LoadAsset{T}(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public T LoadComponent<T>(IAssetReference reference)
			where T : Component
		{
			ThrowIfReferenceIsEmpty(reference);
			ThrowIfSyncLoadingUnsupported();

			if (reference == null)
				ThrowInvalidComponentReference<T>();

			return LoadComponent<T>(reference.AssetReference);
		}

		/// <summary>
		/// Синхронно загрузить GameObject и получить у него выбранный компонент по пути. См. <see cref="LoadAsset{T}(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public T LoadComponent<T>(string path)
			where T : Component
		{
			ThrowIfSyncLoadingUnsupported();

			return LoadComponentByKey<T>(path);
		}

		/// <summary>
		/// Загрузить все ассеты (Label у Addressable). <br/>
		/// Ассеты обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="ReleaseAssets"/>
		/// </summary>
		/// <typeparam name="T">Тип ассетов</typeparam>
		public async UniTask<IList<T>> LoadAssetsAsync<T>(AssetLabelReference reference, CancellationToken cancellationToken,
			IProgress<float> progress = null)
		{
			var labelReference = reference.Reference;
			return await LoadAssetsAsync<T>(labelReference, cancellationToken, progress);
		}

		/// <summary>
		/// Загрузить все ассеты по тегу (Label у Addressable). <br/>
		/// Ассеты обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="ReleaseAssets"/>
		/// </summary>
		/// <typeparam name="T">Тип ассетов</typeparam>
		public async UniTask<IList<T>> LoadAssetsAsync<T>(string tag, CancellationToken cancellationToken,
			IProgress<float> progress = null)
		{
			return await LoadAssetsAsyncByKey<T>(tag, cancellationToken, progress);
		}

		/// <summary>
		/// Загрузить все ассеты по тегу (Label у Addressable). <br/>
		/// Ассеты обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="ReleaseAssets"/>
		/// </summary>
		/// <typeparam name="T">Тип ассетов</typeparam>
		public async UniTask<IList<T>> LoadAssetsAsync<T>(IEnumerable tags, CancellationToken cancellationToken,
			IProgress<float> progress = null)
		{
			return await LoadAssetsAsyncByKey<T>(tags, cancellationToken, progress);
		}

		/// <summary>
		/// Отпустить ассет
		/// </summary>
		public void Release(IAssetReference entry, int? delayMs = 0)
		{
			if (entry == null)
				return;

			var releaseDelayMs = delayMs.GetValueOrDefault().Max(entry.ReleaseDelayMs).Max(0);
			ReleaseAssetByKey(entry.RuntimeKey, releaseDelayMs);
		}

		/// <summary>
		/// Отпустить ассет
		/// </summary>
		public void Release(string path, int delayMs = 0)
		{
			ReleaseAssetByKey(path, delayMs.Max(0));
		}

		/// <summary>
		/// Отпустить ассеты по лейблу
		/// </summary>
		public void ReleaseAssets(AssetLabelReference entry)
		{
			ReleaseAssets(entry.Reference);
		}

		/// <summary>
		/// Отпустить ассеты по тегу
		/// </summary>
		public void ReleaseAssets(string tag)
		{
			ReleaseAssetsByKey(tag);
		}

		public void ReleaseAll()
		{
			ReleaseAllAddressable();
		}

		private static void ThrowIfSyncLoadingUnsupported()
		{
#if UNITY_WEBGL
			const string MESSAGE = "The current synchronous loading implementation does not work on WebGL";
			throw AssetManagementDebug.Exception(MESSAGE);
#endif
		}
	}
}
