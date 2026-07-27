using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AssetManagement.AddressableAssets;
using Cysharp.Threading.Tasks;
using Sapientia.Extensions;
using UnityEngine;

namespace AssetManagement
{
	public partial class AssetProvider : IDisposable
	{
		private bool _initialized;

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
