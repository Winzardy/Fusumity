using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AssetManagement.AddressableAssets
{
	using UnityObject = Object;

	public static class AssetManagementUtility
	{
		public static void ReleaseSafe(this ref AsyncOperationHandle handle)
		{
			if (handle.IsValid())
				Addressables.Release(handle);
		}

		public static void ReleaseSafe<T>(this ref AsyncOperationHandle<T> handle)
		{
			if (handle.IsValid())
				Addressables.Release(handle);
		}

		/// <summary>
		/// Метод в основном для дебага, вернет нулл если вызывается не в редакторе
		/// </summary>
		public static UnityObject GetEditorAssetSafe(this AssetReference assetReference)
		{
#if UNITY_EDITOR
			return assetReference.editorAsset;
#endif
			return null;
		}

		public static bool IsRuntimeValid(this AssetReference assetReference)
		{
			if (!assetReference.RuntimeKeyIsValid())
				return false;

#if DEBUG
			var key = assetReference.RuntimeKey;
			if (key == null)
				return false;

			foreach (var locator in Addressables.ResourceLocators)
			{
				if (locator.Locate(key, typeof(object), out var locations) && locations is {Count: > 0})
					return true;
			}

			return false;
#else
			return true;
#endif
		}
	}
}
