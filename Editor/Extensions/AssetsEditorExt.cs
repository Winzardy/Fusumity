using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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

		public static T GetAssetOfType<T>(string[] searchInFolders = null, HashSet<T> exclude = null) where T: Object
		{
			var guids = AssetDatabase.FindAssets($"t: {typeof(T).Name}", searchInFolders);

			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath<T>(path);
				if (exclude != null && exclude.Contains(asset))
					continue;

				return asset;
			}

			return null;
		}

		public static List<T> GetAssetsOfType<T>(string[] searchInFolders = null, HashSet<T> exclude = null) where T: Object
		{
			var guids = AssetDatabase.FindAssets($"t: {typeof(T).Name}", searchInFolders);
			var assets = new List<T>(guids.Length);

			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath<T>(path);
				if (exclude != null && exclude.Contains(asset))
					continue;

				assets.Add(asset);
			}

			return assets;
		}

		public static List<(Object asset, string path)> GetAssetsOfComponentTypeWithPath(Type type, string[] searchInFolders = null)
		{
			var gameObjectType = typeof(GameObject);

			var guids = AssetDatabase.FindAssets($"t: {gameObjectType.Name}", searchInFolders);
			var assets = new List<(Object asset, string path)>(guids.Length);

			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath(path, gameObjectType);

				if ((asset as GameObject)?.GetComponent(type) == null)
					continue;

				assets.Add((asset, path));
			}

			return assets;
		}

		public static List<(Object asset, string path)> GetAssetsOfTypeWithPath(Type type, string[] searchInFolders = null)
		{
			var guids = AssetDatabase.FindAssets($"t: {type.Name}", searchInFolders);
			var assets = new List<(Object asset, string path)>(guids.Length);

			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath(path, type);

				assets.Add((asset, path));
			}

			return assets;
		}

		public static void Rename(this ScriptableObject scriptableObject, string newName)
		{
			var assetPath = AssetDatabase.GetAssetPath(scriptableObject.GetInstanceID());
			AssetDatabase.RenameAsset(assetPath, newName);
			AssetDatabase.SaveAssets();
		}

		public static void SaveChanges(this Object unityObject)
		{
			EditorUtility.SetDirty(unityObject);
			AssetDatabase.SaveAssetIfDirty(unityObject);
		}
	}
}
