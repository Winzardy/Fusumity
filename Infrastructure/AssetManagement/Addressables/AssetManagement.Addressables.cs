using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AssetManagement.AddressableAssets;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AssetManagement
{
	using UnityObject = UnityEngine.Object;
	using UnityAssetReference = UnityEngine.AddressableAssets.AssetReference;
	using UnityAssetLabelReference = UnityEngine.AddressableAssets.AssetLabelReference;

	/// <summary>
	/// Заметка! <br/>
	/// При работае с Addressables было выявлено, что
	/// при выгрузке ассета (Release) он может остаться висеть в памяти, события которые реально его выгружают:<br/>
	/// - Все handle'ы группы отпущены (release) <br/>
	/// - [иногда] Подгрузка нового элемента группы, который до этого был полностью выгружен или вообще не подгружался
	/// (с какими-то ассетами работает, с какими-то нет, природа этого явления неясна)
	///
	/// <br/>
	/// Решение: Группировать ассеты как можно чаще...
	/// Проблема группировки ассетов, что создаются потенциальные условия для дубликатов...<br/>
	/// К счастью при билде Addressables в конце выдается репорт и в репорте можно посмотреть какие ассеты дублируются
	/// </summary>
	public partial class AssetProvider
	{
		//Активные ассеты
		//object в виде ключя из-за Addressables...
		private Dictionary<object, AssetContainer> _keyToAssetContainer = new(16);
		private Dictionary<object, AssetsContainer> _keyToAssetCollectionContainer = new(2);

		private void DisposeAddressable()
		{
			ReleaseAllAddressable();

			_keyToAssetContainer = null;
			_keyToAssetCollectionContainer = null;
		}

		private void ReleaseAllAddressable()
		{
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

		private async UniTask<T> LoadAssetAsyncByKey<T>(object key, CancellationToken cancellationToken,
			UnityObject context = null, IProgress<float> progress = null)
		{
			var usedAsset = await FindOrWaitUsedAssetByKeyAsync<T>(key, cancellationToken, progress);

			if (!ReferenceEquals(usedAsset, null))
				return usedAsset;

			var handle = Addressables.LoadAssetAsync<T>(key);

			if (!handle.IsValid())
			{
				AssetManagementDebug.LogError($"Failed to load asset: handle by key [ {key} ] is invalid", context);
				return default;
			}

			_keyToAssetContainer[key] = new AssetContainer(key, handle);

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

		private void Release(UnityAssetReference assetReference)
		{
			ReleaseAssetByKey(assetReference.RuntimeKey);
		}

		//Интересно, что под капотом AssetReference.RuntimeKey виртуальный метод внутри которого string...
		private void ReleaseAssetByKey(object key)
		{
			if (!_initialized)
				return;

			if (!_keyToAssetContainer.TryGetValue(key, out var container))
				return;

			if (!container.Release())
				return;

			_keyToAssetContainer.Remove(key);
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

			if (!container.Release())
				return;

			_keyToAssetCollectionContainer.Remove(key);
		}

		#region Sync

		private T LoadAsset<T>(UnityAssetReference assetReference)
		{
			if (assetReference == null)
				ThrowInvalidAssetReference<T>();

			var context = assetReference.GetEditorAssetSafe();
			if (!assetReference.IsRuntimeValid())
			{
				ThrowInvalidAssetReference<T>(context);
			}

			var key = assetReference.RuntimeKey;
			return LoadAssetByKey<T>(key, context);
		}

		private T LoadComponent<T>(UnityAssetReference assetReference)
		{
			if (assetReference == null)
				ThrowInvalidComponentReference<T>();

			var context = assetReference.GetEditorAssetSafe();
			if (!assetReference.IsRuntimeValid())
			{
				ThrowInvalidComponentReference<T>(context);
			}

			var key = assetReference.RuntimeKey;
			return LoadComponentByKey<T>(key, context);
		}

		private T LoadComponentByKey<T>(object key, UnityObject context = null)
		{
			var asset = LoadAssetByKey<GameObject>(key, context);

			if (!asset.TryGetComponent(out T component))
			{
				ReleaseAssetByKey(key);
				ThrowInvalidComponentReference<T>(context);
			}

			return component;
		}

		private T LoadAssetByKey<T>(object key, UnityObject context = null)
		{
			//Уже загружен/грузится — форсим завершение синхронно и переиспользуем контейнер
			if (_keyToAssetContainer.TryGetValue(key, out var used))
				return used.GetAsset<T>();

			var handle = Addressables.LoadAssetAsync<T>(key);

			if (!handle.IsValid())
			{
				AssetManagementDebug.LogError($"Failed to load asset: handle by key [ {key} ] is invalid", context);
				return default;
			}

			_keyToAssetContainer[key] = new AssetContainer(key, handle);

			var asset = handle.WaitForCompletion();

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

			return asset;
		}

		#endregion
	}
}
