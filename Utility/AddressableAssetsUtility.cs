using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Fusumity.AddressableAssets
{
	using UnityObject = Object;

	public static class AddressableAssetsUtility
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
	}
}
