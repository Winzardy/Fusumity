using AssetManagement.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AssetManagement
{
	using UnityObject = UnityEngine.Object;
	using UnityAssetReference = UnityEngine.AddressableAssets.AssetReference;

	public partial class AssetProvider
	{
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
			// Уже загружен/грузится — форсим завершение синхронно и переиспользуем контейнер

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
	}
}
