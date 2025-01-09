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
		public static void DeleteAssetsWithoutDependencies<T>() where T: Object
		{
			AssetDatabase.SaveAssets();

			var guids = AssetDatabase.FindAssets($"t: {typeof(T).Name}");
			var paths = new HashSet<string>(guids.Length);
			foreach (var guid in guids)
			{
				paths.Add(AssetDatabase.GUIDToAssetPath(guid));
			}

			var allAssetPaths = AssetDatabase.GetAllAssetPaths();
			var dependencies = new HashSet<string>();
			foreach (var path in allAssetPaths)
			{
				if (paths.Contains(path))
					continue;

				foreach (var dependency in AssetDatabase.GetDependencies(path, true))
					dependencies.Add(dependency);
			}

			var assetsToDelete = new List<string>();
			foreach (var path in paths)
			{
				if (dependencies.Contains(path))
					continue;
				assetsToDelete.Add(path);
			}

			foreach (var path in assetsToDelete)
			{
				AssetDatabase.DeleteAsset(path);
			}
		}

		public static GUID GetAssetGuid<T>(this T asset) where T: Object
		{
			var assetPath = AssetDatabase.GetAssetPath(asset);
			return AssetDatabase.GUIDFromAssetPath(assetPath);
		}

		public static GUID GetAssetGuid<T>(this T asset, out string assetPath) where T: Object
		{
			assetPath = AssetDatabase.GetAssetPath(asset);
			return AssetDatabase.GUIDFromAssetPath(assetPath);
		}

		public static string GetAssetPath<T>(this T asset) where T: Object
		{
			return AssetDatabase.GetAssetPath(asset);
		}

		public static string GetAssetFolder<T>(this T asset) where T: Object
		{
			var assetPath = AssetDatabase.GetAssetPath(asset);
			var folderPath = Path.GetDirectoryName(assetPath);
			return folderPath;
		}

		public static string GetPathOfAssetType<T>(string[] searchInFolders = null) where T : Object
		{
			var guid = GetGuidOfAssetType<T>(searchInFolders);
			var databasePath = AssetDatabase.GUIDToAssetPath(guid);
			return databasePath;
		}

		public static string GetGuidOfAssetType<T>(string[] searchInFolders = null) where T : Object
		{
			var guid = AssetDatabase.FindAssets($"t: {typeof(T).Name}", searchInFolders)[0];
			return guid;
		}

		public static string[] GetGuidsOfAssetType<T>(string[] searchInFolders = null) where T : Object
		{
			var guids = AssetDatabase.FindAssets($"t: {typeof(T).Name}", searchInFolders);
			return guids;
		}

		public static T GetAssetByGuid<T>(string guid) where T: Object
		{
			if (string.IsNullOrEmpty(guid))
				return null;
			var assetPath = AssetDatabase.GUIDToAssetPath(guid);
			return AssetDatabase.LoadAssetAtPath<T>(assetPath);
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

		public static List<T> GetAssetsOfType<T>(out string[] guids, string[] searchInFolders = null, HashSet<T> exclude = null) where T: Object
		{
			guids = AssetDatabase.FindAssets($"t: {typeof(T).Name}", searchInFolders);
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
		}

		public static void SaveChanges(this Object unityObject)
		{
			EditorUtility.SetDirty(unityObject);
			AssetDatabase.SaveAssetIfDirty(unityObject);
		}
	}
}
