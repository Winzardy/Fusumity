using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AssetManagement.AddressableAssets;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AssetManagement
{
	using UnityObject = UnityEngine.Object;
	using UnityAssetReference = UnityEngine.AddressableAssets.AssetReference;
	using UnityAssetLabelReference = UnityEngine.AddressableAssets.AssetLabelReference;

	/// <summary>
	/// Поведение Addressables при выгрузке ассетов зависит не только от Release(handle),
	/// но и от того, как ассеты упакованы (Group, Bundle Layout, Pack Mode).
	/// <br/><br/>
	/// Наблюдение:
	/// даже после Addressables.Release(handle) ассет может оставаться в памяти,
	/// если:
	/// - существуют другие живые ссылки (handle'ы, зависимости, сцены, кэшированные references)
	/// - ассет находится в одном AssetBundle вместе с другими объектами (Pack Mode = Pack Together / Pack Separately)
	/// - сам AssetBundle остаётся загруженным из-за других активных ассетов внутри него
	/// <br/><br/>
	/// Важно понимать:
	/// Addressables не выгружает отдельные ассеты напрямую — выгружается AssetBundle.
	/// Поэтому “ассет не выгрузился” обычно означает, что:
	/// - bundle ещё нужен другим ассетам
	/// - или не был реально unloaded сам bundle
	/// <br/><br/>
	/// Дополнительно:
	/// поведение может отличаться при повторной загрузке, потому что Unity может:
	/// - переиспользовать уже загруженный AssetBundle
	/// - пересобрать внутренние зависимости
	/// <br/><br/>
	/// Вывод:
	/// оптимизация выгрузки достигается не “частой группировкой”, а корректной конфигурацией:
	/// - правильное разделение Addressables Groups
	/// - осознанный выбор Pack Mode (Together / Separately)
	/// - контроль shared dependencies
	/// - отсутствие лишних удерживающих ссылок
	/// <br/><br/>
	/// В билде Addressables можно проверить дубли и shared dependencies через Build Report.
	/// </summary>
	public partial class AssetProvider
	{
		private AsyncOperationHandle<IResourceLocator> _addressableInitializationHandle;

		//Активные ассеты
		//object в виде ключя из-за Addressables...
		private Dictionary<object, AssetContainer> _keyToAssetContainer = new(16);
		private Dictionary<object, AssetsContainer> _keyToAssetCollectionContainer = new(2);
		private List<AssetContainer> _delayedReleaseContainers;
		private bool _delayedReleaseSubscribed;

		private void DisposeAddressable()
		{
			UnSubscribeDelayedAssetReleases();
			ReleaseAllAddressable();
			_addressableInitializationHandle.ReleaseSafe();
			_addressableInitializationHandle = default;

			_keyToAssetContainer = null;
			_keyToAssetCollectionContainer = null;
		}

		public UniTask WarmUpAddressables(CancellationToken cancellationToken = default)
		{
			if (_addressableInitializationHandle.IsValid())
				return _addressableInitializationHandle.ToUniTask(cancellationToken: cancellationToken);

			_addressableInitializationHandle = Addressables.InitializeAsync(false);
			return _addressableInitializationHandle.ToUniTask(cancellationToken: cancellationToken);
		}

		public async UniTask DownloadDependenciesAsync(AssetLabelReference reference,
			CancellationToken cancellationToken = default)
		{
			await WarmUpAddressables(cancellationToken);

			var handle = Addressables.DownloadDependenciesAsync(reference.ToString(), false);
			try
			{
				await handle.ToUniTask(cancellationToken: cancellationToken);

				if (handle.Status == AsyncOperationStatus.Failed)
					throw handle.OperationException
					      ?? AssetManagementDebug.Exception($"Failed to download dependencies by label [ {reference} ]");
			}
			finally
			{
				handle.ReleaseSafe();
			}
		}

		private async UniTask EnsureAddressableInitializedAsync(CancellationToken cancellationToken)
		{
			await WarmUpAddressables(cancellationToken);
		}

		private void ReleaseAllAddressable()
		{
			ClearDelayedAssetReleases();

			foreach (var container in _keyToAssetContainer.Values)
			{
				container.Dispose();
			}

			_keyToAssetContainer.Clear();

			foreach (var container in _keyToAssetCollectionContainer.Values)
			{
				container.Dispose();
			}

			_keyToAssetCollectionContainer.Clear();
		}

		private static void ThrowInvalidAssetReference<T>(UnityObject context = null)
		{
			var exception = AssetManagementDebug.Exception($"Invalid asset reference by type [ {typeof(T)} ]");
			AssetManagementDebug.LogException(exception, context);
			throw exception;
		}

		private static void ThrowInvalidComponentReference<T>(UnityObject context = null)
		{
			var exception = AssetManagementDebug.Exception($"Invalid component reference by type [ {typeof(T)} ]");
			AssetManagementDebug.LogException(exception, context);
			throw exception;
		}

		private async UniTask<T> LoadAssetAsync<T>(UnityAssetReference assetReference, CancellationToken cancellationToken,
			IProgress<float> progress = null)
		{
			if (assetReference == null)
				ThrowInvalidAssetReference<T>();

			await EnsureAddressableInitializedAsync(cancellationToken);

			var context = assetReference.GetEditorAssetSafe();
			if (!assetReference.IsRuntimeValid())
			{
				ThrowInvalidAssetReference<T>(context);
			}

			var key = assetReference.RuntimeKey;
			return await LoadAssetAsyncByKey<T>(key, cancellationToken, context, progress);
		}

		private async UniTask<T> LoadComponentAsync<T>(UnityAssetReference assetReference, CancellationToken cancellationToken,
			IProgress<float> progress = null)
		{
			if (assetReference == null)
				ThrowInvalidComponentReference<T>();

			await EnsureAddressableInitializedAsync(cancellationToken);

			var context = assetReference.GetEditorAssetSafe();
			if (!assetReference.IsRuntimeValid())
			{
				ThrowInvalidComponentReference<T>(context);
			}

			var key = assetReference.RuntimeKey;
			return await LoadComponentByKeyAsync<T>(key, cancellationToken, context, progress);
		}

		private async UniTask<T> LoadComponentByKeyAsync<T>(object key, CancellationToken cancellationToken,
			UnityObject context = null, IProgress<float> progress = null)
		{
			var asset = await LoadAssetAsyncByKey<GameObject>(key, cancellationToken, context, progress);

			if (!asset.TryGetComponent(out T component))
			{
				ReleaseAssetByKey(key);
				ThrowInvalidComponentReference<T>(context);
			}

			return component;
		}

		private async UniTask<T> FindOrWaitUsedAssetByKeyAsync<T>(object key, CancellationToken cancellationToken,
			IProgress<float> progress = null)
		{
			if (_keyToAssetContainer.TryGetValue(key, out var container))
				return await container.GetAssetAsync<T>(cancellationToken, progress);

			return default;
		}

		private AssetContainer AcquireAssetContainerByKey<T>(object key, UnityObject context = null)
		{
			if (_keyToAssetContainer.TryGetValue(key, out var container))
			{
				container.Retain();
				return container;
			}

			var handle = Addressables.LoadAssetAsync<T>(key);

			if (!handle.IsValid())
			{
				AssetManagementDebug.LogError($"Failed to load asset: handle by key [ {key} ] is invalid", context);
				throw AssetManagementDebug.Exception("Failed to load asset");
			}

			container = new AssetContainer(this, key, handle);
			_keyToAssetContainer[key] = container;
			return container;
		}

		private async UniTask<T> LoadAssetAsyncByKey<T>(object key, CancellationToken cancellationToken,
			UnityObject context = null, IProgress<float> progress = null)
		{
			await EnsureAddressableInitializedAsync(cancellationToken);

			var usedAsset = await FindOrWaitUsedAssetByKeyAsync<T>(key, cancellationToken, progress);

			if (!ReferenceEquals(usedAsset, null))
				return usedAsset;

			var handle = Addressables.LoadAssetAsync<T>(key);

			if (!handle.IsValid())
			{
				AssetManagementDebug.LogError($"Failed to load asset: handle by key [ {key} ] is invalid", context);
				return default;
			}

			_keyToAssetContainer[key] = new AssetContainer(this, key, handle);

			var (isCanceled, asset) = await handle
				.ToUniTask(progress, cancellationToken: cancellationToken)
				.SuppressCancellationThrow();

			if (isCanceled)
			{
				ReleaseAssetByKey(key);
				AssetManagementDebug.LogWarning($"Cancelled to load asset for key [ {key} ]" +
					"\nAddressable:" +
					$"\n	Exception: {handle.OperationException}" +
					$"\n	Status: {handle.Status}" +
					$"\n	Debug: {handle.DebugName}"
					, context);
				cancellationToken.ThrowIfCancellationRequested();
			}

			if (handle.Status != AsyncOperationStatus.Succeeded)
			{
				ReleaseAssetByKey(key);
				AssetManagementDebug.LogError($"Failed to load asset for key [ {key} ]" +
					"\nAddressable:" +
					$"\n	Exception: {handle.OperationException}" +
					$"\n	Status: {handle.Status}" +
					$"\n	Debug: {handle.DebugName}"
					, context);
				throw AssetManagementDebug.Exception("Failed to load asset");
			}

			//Гарантируем финальный репорт: MoveNext может не успеть выдать ровно 1 до завершения хэндла
			progress?.Report(1f);
			return asset;
		}

		private async UniTask<IList<T>> LoadAssetsAsync<T>(UnityAssetLabelReference labelReference, CancellationToken cancellationToken,
			IProgress<float> progress = null)
		{
			if (!labelReference.RuntimeKeyIsValid())
			{
				AssetManagementDebug.LogError("Label reference invalid");
				return null;
			}

			var key = labelReference.RuntimeKey;
			return await LoadAssetsAsyncByKey<T>(key, cancellationToken, progress);
		}

		private async UniTask<IList<T>> LoadAssetsAsyncByKey<T>(object key, CancellationToken cancellationToken,
			IProgress<float> progress = null)
		{
			await EnsureAddressableInitializedAsync(cancellationToken);

			var usedAssets = await FindUsedAssetsByKeyAsync<T>(key, cancellationToken, progress);

			if (usedAssets != null)
				return usedAssets;

			var handle = Addressables.LoadAssetsAsync<T>(key, null);
			_keyToAssetCollectionContainer[key] = new AssetsContainer(key, handle);

			var (isCanceled, assets) = await handle.ToUniTask(progress, cancellationToken: cancellationToken)
				.SuppressCancellationThrow();

			if (isCanceled)
			{
				ReleaseAssetsByKey(key);
				cancellationToken.ThrowIfCancellationRequested();
			}

			progress?.Report(1f);
			return assets;
		}

		private async UniTask<IList<T>> LoadAssetsAsyncByKey<T>(IEnumerable keys, CancellationToken cancellationToken,
			IProgress<float> progress = null)
		{
			await EnsureAddressableInitializedAsync(cancellationToken);

			var usedAssets = await FindUsedAssetsByKeyAsync<T>(keys, cancellationToken, progress);

			if (usedAssets != null)
				return usedAssets;

			var handle = Addressables.LoadAssetsAsync<T>(keys, null);
			_keyToAssetCollectionContainer[keys] = new AssetsContainer(keys, handle);

			var (isCanceled, assets) = await handle.ToUniTask(progress, cancellationToken: cancellationToken)
				.SuppressCancellationThrow();

			if (isCanceled)
			{
				ReleaseAssetsByKey(keys);
				cancellationToken.ThrowIfCancellationRequested();
			}

			progress?.Report(1f);
			return assets;
		}

		private async UniTask<IList<T>> FindUsedAssetsByKeyAsync<T>(object key, CancellationToken cancellationToken,
			IProgress<float> progress = null)
		{
			if (_keyToAssetCollectionContainer.TryGetValue(key, out var container))
			{
				return await container.GetAssetsAsync<T>(cancellationToken, progress);
			}

			return null;
		}

		private void ReleaseAssetByKey(object key, int delayMs = 0)
		{
			if (!_initialized || key == null)
				return;

			if (!_keyToAssetContainer.TryGetValue(key, out var container))
				return;

			ReleaseAssetContainer(container, delayMs);
		}

		private void ReleaseAssetContainer(AssetContainer container, int delayMs = 0)
		{
			if (!_initialized || container == null)
				return;

			if (!IsCurrentAssetContainer(container))
				return;

			if (container.ReleaseUsage(delayMs, Time.unscaledTimeAsDouble))
			{
				ReleaseAssetContainerImmediately(container);
				return;
			}

			if (container.IsReleasePending)
				TrackDelayedRelease(container);
		}

		private bool IsCurrentAssetContainer(AssetContainer container)
		{
			var key = container.Key;
			return _keyToAssetContainer != null &&
				key != null &&
				_keyToAssetContainer.TryGetValue(key, out var current) &&
				ReferenceEquals(current, container);
		}

		private void ReleaseAssetContainerImmediately(AssetContainer container)
		{
			if (!IsCurrentAssetContainer(container))
				return;

			var key = container.Key;
			_keyToAssetContainer.Remove(key);
			container.Dispose();
		}

		private void TrackDelayedRelease(AssetContainer container)
		{
			if (!container.TryTrackRelease())
				return;

			_delayedReleaseContainers ??= new List<AssetContainer>(4);
			_delayedReleaseContainers.Add(container);
		}

		private void UpdateDelayedAssetReleases()
		{
			if (!_initialized || _delayedReleaseContainers == null)
				return;

			var currentTime = Time.unscaledTimeAsDouble;
			for (var i = _delayedReleaseContainers.Count - 1; i >= 0; i--)
			{
				var container = _delayedReleaseContainers[i];
				if (!container.IsReleasePending || !IsCurrentAssetContainer(container))
				{
					RemoveDelayedReleaseAt(i);
					continue;
				}

				if (!container.IsReleaseReady(currentTime))
					continue;

				RemoveDelayedReleaseAt(i);
				ReleaseAssetContainerImmediately(container);
			}
		}

		private void RemoveDelayedReleaseAt(int index)
		{
			var container = _delayedReleaseContainers[index];
			var lastIndex = _delayedReleaseContainers.Count - 1;

			if (index != lastIndex)
				_delayedReleaseContainers[index] = _delayedReleaseContainers[lastIndex];

			_delayedReleaseContainers.RemoveAt(lastIndex);
			container.StopTrackingRelease();
		}

		private void ClearDelayedAssetReleases()
		{
			if (_delayedReleaseContainers == null)
				return;

			foreach (var container in _delayedReleaseContainers)
				container.StopTrackingRelease();

			_delayedReleaseContainers.Clear();
		}

		private void SubscribeDelayedAssetReleases()
		{
			if (_delayedReleaseSubscribed)
				return;

			_delayedReleaseSubscribed = true;
			UnityLifecycle.UnscaledEachSecondEvent.Subscribe(UpdateDelayedAssetReleases);
		}

		private void UnSubscribeDelayedAssetReleases()
		{
			if (!_delayedReleaseSubscribed)
				return;

			_delayedReleaseSubscribed = false;
			UnityLifecycle.UnscaledEachSecondEvent.UnSubscribe(UpdateDelayedAssetReleases);
		}

		private void ReleaseAssets(UnityAssetLabelReference labelReference)
		{
			ReleaseAssetsByKey(labelReference.RuntimeKey);
		}

		private void ReleaseAssetsByKey(object key)
		{
			if (!_initialized)
				return;

			if (!_keyToAssetCollectionContainer.TryGetValue(key, out var container))
				return;

			if (!container.ReleaseUsage())
				return;

			_keyToAssetCollectionContainer.Remove(key);
		}
	}
}
