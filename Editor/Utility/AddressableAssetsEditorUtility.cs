using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Fusumity.Editor.Utility
{
	public static class AddressableAssetsEditorUtility
	{
		public static T GetPrefab<T>(this AssetReference reference) where T : MonoBehaviour
		{
			var path = AssetDatabase.GUIDToAssetPath(reference.AssetGUID);
			return AssetDatabaseUtility.GetPrefab<T>(path);
		}
	}
}
