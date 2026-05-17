#if UNITY_EDITOR
using AssetManagement.AddressableAssets.Editor;
using UnityEngine;

namespace AssetManagement
{
	using UnityObject = UnityEngine.Object;

	public static partial class AssetReferenceUtility
	{
		public static void SetEditorAsset<T>(this IAssetReference reference, T value, string address = null, string group = null,
			string label = null, bool createGroupIfNonExistent = true) where T : UnityObject
		{
			if (value && !value.IsAddressable())
				value.MakeAddressable(group, address, label, createGroupIfNonExistent);

			reference.AssetReference.SetEditorAsset(value);
			reference.AssetReference.SetEditorSubObject(value);
		}

		public static void SetEditorAsset<T>(this ComponentReference<T> reference, GameObject value, string address = null,
			string group = null,
			string label = null, bool createGroupIfNonExistent = true) where T : Component
		{
			if (value != null && !value.TryGetComponent(out T _))
			{
				AssetManagementDebug.LogError($"Component of type <b>{typeof(T).Name}</b> was not found on GameObject [ {value.name} ]", value);
				return;
			}

			if (value && !value.IsAddressable())
				value.MakeAddressable(group, address, label, createGroupIfNonExistent);

			reference.AssetReference.SetEditorAsset(value);
			//entry.AssetReference.SetEditorSubObject(value);
		}

		public static AssetReference<T> CreateAssetReferenceEntry<T>(this T asset, string address = null, string group = null,
			string label = null) where T : UnityObject
		{
			var entry = new AssetReference<T>
			{
				assetReference = new UnityEngine.AddressableAssets.AssetReferenceT<T>(null)
			};

			entry.SetEditorAsset(asset, address, group, label);
			return entry;
		}
	}
}

#endif
