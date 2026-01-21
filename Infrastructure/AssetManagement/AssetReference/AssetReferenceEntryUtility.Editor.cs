#if UNITY_EDITOR
using AssetManagement.AddressableAssets.Editor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetManagement
{
	public static partial class AssetReferenceEntryUtility
	{
		public static void SetEditorAsset<T>(this IAssetReferenceEntry entry, T value, string address = null, string group = null,
			string label = null, bool createGroupIfNonExistent = true) where T : Object
		{
			if (value && !value.IsAddressable())
				value.MakeAddressable(group, address, label, createGroupIfNonExistent);

			entry.AssetReference.SetEditorAsset(value);
			entry.AssetReference.SetEditorSubObject(value);
		}

		public static void SetEditorAsset<T>(this ComponentReferenceEntry<T> entry, GameObject value, string address = null,
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

			entry.AssetReference.SetEditorAsset(value);
			//entry.AssetReference.SetEditorSubObject(value);
		}

		public static AssetReferenceEntry<T> CreateAssetReferenceEntry<T>(this T asset, string address = null, string group = null,
			string label = null) where T : Object
		{
			var entry = new AssetReferenceEntry<T>();
			entry.SetEditorAsset(asset, address, group, label);
			return entry;
		}
	}
}

#endif
