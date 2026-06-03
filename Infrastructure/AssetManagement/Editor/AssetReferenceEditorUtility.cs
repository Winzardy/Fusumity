using AssetManagement.AddressableAssets.Editor;

namespace AssetManagement.Editor
{
	public static class AssetReferenceEditorUtility
	{
		public static bool IsEmpty(this IAssetReference reference) =>
			reference is {AssetReference: null} || reference.AssetReference.IsEmpty();

		public static bool IsPopulated(this IAssetReference reference) =>
			reference is {AssetReference: not null} && reference.AssetReference.IsPopulated();
	}
}
