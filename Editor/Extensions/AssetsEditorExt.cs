using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Extensions
{
	public static class AssetsEditorExt
	{
		public static string GetAssetFolder<T>(this T asset) where T: Object
		{
			var assetPath = AssetDatabase.GetAssetPath(asset);
			var folderPath = Path.GetDirectoryName(assetPath);
			return folderPath;
		}

		public static List<T> GetAssetsOfType<T>() where T: Object
		{
			var guids = AssetDatabase.FindAssets($"t: {typeof(T).Name}");
			var assets = new List<T>(guids.Length);

			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath<T>(path);

				assets.Add(asset);
			}

			return assets;
		}

		public static void Rename(this ScriptableObject scriptableObject, string newName)
		{
			var assetPath =  AssetDatabase.GetAssetPath(scriptableObject.GetInstanceID());
			AssetDatabase.RenameAsset(assetPath, newName);
			AssetDatabase.SaveAssets();
		}

		public static void SaveChanges(this Object unityObject)
		{
			EditorUtility.SetDirty(unityObject);
			AssetDatabase.SaveAssets();
		}
	}
}