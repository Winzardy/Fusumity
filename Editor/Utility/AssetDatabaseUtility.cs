using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sapientia.Collections;
using Sapientia.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Utility
{
	using UnityObject = UnityEngine.Object;

	public static class AssetDatabaseUtility
	{
		public static string ToGuid(this UnityObject obj)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			return AssetDatabase.AssetPathToGUID(path);
		}

		public static UnityObject ToAsset(this string guid)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			return AssetDatabase.LoadAssetAtPath<UnityObject>(path);
		}

		public static T ToAsset<T>(this string guid)
			where T : UnityObject
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			return AssetDatabase.LoadAssetAtPath<T>(path);
		}

		public static IEnumerable<GameObject> LoadPrefabs(string[] searchInFolders = null, string filter = "t:prefab")
		{
			return AssetDatabase.FindAssets(filter, searchInFolders)
			   .Select(AssetDatabase.GUIDToAssetPath)
			   .Select(AssetDatabase.LoadAssetAtPath<GameObject>);
		}

		public static T GetAsset<T>(Func<string, bool> pathPredicate) where T : UnityObject
		{
			var guids = GetAssetsGuids<T>();

			for (int i = 0; i < guids.Length; i++)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[i]);
				if (pathPredicate.Invoke(path))
				{
					var asset = AssetDatabase.LoadAssetAtPath<T>(path);
					return asset;
				}
			}

			return null;
		}

		public static UnityObject GetAsset(Type type, string path = null)
		{
			var guids = GetAssetsGuids<UnityObject>(path);

			if (guids.IsNullOrEmpty())
			{
				Debug.LogError($"Could not find valid guid for asset of type: [ {type} ] at path: [ {path} ]");
				return null;
			}

			if (guids.Length > 1)
			{
				Debug.LogWarning($"Found more than one asset of type: [ {type} ]");
			}

			var assetPath = AssetDatabase.GUIDToAssetPath(guids.First());
			return AssetDatabase.LoadAssetAtPath(assetPath, type);
		}

		public static void GetAsset<T>(out T asset, string path = null) where T : UnityObject
		{
			asset = GetAsset<T>(path);
		}

		public static T GetAsset<T>(string path = null, bool error = true) where T : UnityObject
		{
			var guids = GetAssetsGuids<T>(path);

			if (guids.IsNullOrEmpty())
			{
				if (error)
					Debug.LogError($"Could not find valid guid for asset of type: [ {typeof(T)} ] at path: [ {path} ]");
				return null;
			}

			if (guids.Length > 1)
			{
				if (error)
					Debug.LogWarning($"Found more than one asset of type: [ {typeof(T)} ]");
			}

			return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids.First()));
		}

		public static UnityObject[] GetAssets(Type type, string path = null)
		{
			var guids = GetAssetsGuids(type, path);

			var assets = new UnityObject[guids.Length];
			for (int i = 0; i < guids.Length; i++)
			{
				UnityObject asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), type);
				assets[i] = asset;
			}

			return assets;
		}

		public static T[] GetAssets<T>(string path = null) where T : UnityObject
		{
			var guids = GetAssetsGuids<T>(path);

			var assets = new T[guids.Length];
			for (int i = 0; i < guids.Length; i++)
			{
				T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
				assets[i] = asset;
			}

			return assets;
		}

		public static T GetPrefab<T>(string path) where T : MonoBehaviour
		{
			if (path.IsNullOrEmpty())
			{
				Debug.LogWarning($"Empty path for prefab of type: [ {typeof(T)} ]");
				return null;
			}

			var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (asset != null && PrefabUtility.IsPartOfAnyPrefab(asset))
			{
				var component = asset.GetComponent<T>();
				if (component == null)
				{
					Debug.LogWarning(
						$"Could not find valid component of type [ {typeof(T)} ] " +
						$"on prefab at path: [ {path} ]");
				}

				return component;
			}

			Debug.LogWarning($"Could not find prefab at path: [ {path} ]");
			return null;
		}

		public static List<T> GetComponentsFromPrefabs<T>(string path = null, bool includeChildren = false)
		{
			var guids = GetAssetsGuids<GameObject>(path);

			var assets = new List<T>();
			for (int i = 0; i < guids.Length; i++)
			{
				var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
				if (PrefabUtility.IsPartOfAnyPrefab(asset))
				{
					if (includeChildren)
					{
						var components = asset.GetComponentsInChildren<T>();
						if (components != null)
						{
							assets.AddRange(components);
						}
					}
					else
					{
						var component = asset.GetComponent<T>();
						if (component != null)
						{
							assets.Add(component);
						}
					}
				}
			}

			return assets;
		}

		public static List<GameObject> GetPrefabsWithComponent<T>(string path = null)
		{
			return GetPrefabsWithComponent(typeof(T), path);
		}

		public static List<GameObject> GetPrefabsWithComponent(Type type, string path = null)
		{
			var guids = GetAssetsGuids<GameObject>(path);

			var assets = new List<GameObject>();
			for (int i = 0; i < guids.Length; i++)
			{
				var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
				if (PrefabUtility.IsPartOfAnyPrefab(asset))
				{
					if (asset.GetComponentInChildren(type))
						assets.Add(asset);
				}
			}

			return assets;
		}

		public static List<GameObject> GetPrefabsOfType(string type, string path = null)
		{
			var guids = GetAssetsGuids<GameObject>(path);

			var assets = new List<GameObject>();
			for (int i = 0; i < guids.Length; i++)
			{
				var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
				if (PrefabUtility.IsPartOfAnyPrefab(asset))
				{
					var component = asset.GetComponent(type);
					if (component != null)
					{
						assets.Add(asset);
					}
				}
			}

			return assets;
		}

		public static T CreateScriptableObject<T>(string path, string assetName, bool saveAssets = true) where T : ScriptableObject
		{
			return (T)CreateScriptableObject(typeof(T), path, assetName, saveAssets);
		}

		public static T CreateScriptableObject<T>(Type type, string path, string assetName, bool saveAssets = true)
			where T : ScriptableObject
		{
			return (T)CreateScriptableObject(type, path, assetName, saveAssets);
		}

		public static ScriptableObject CreateScriptableObject(Type type, string path, string assetName, bool saveAssets = true)
		{
			string absolutePath = path.GetAbsolutePath();
			if (!Directory.Exists(absolutePath))
			{
				Directory.CreateDirectory(absolutePath);
			}

			ScriptableObject asset = ScriptableObject.CreateInstance(type);
			assetName = assetName.Contains(".asset") ? assetName : assetName + ".asset";

			string finalPath = $"{path}/{assetName}";
			AssetDatabase.CreateAsset(asset, finalPath);

			if (saveAssets)
			{
				AssetDatabase.SaveAssets();
			}

			return asset;
		}

		public static string[] GetAssetsGuids<T>(string path = null)
		{
			return GetAssetsGuids(typeof(T), path);
		}

		public static string[] GetAssetsGuids(Type type, string path = null)
		{
			return string.IsNullOrEmpty(path)
				? AssetDatabase.FindAssets($"t:{type.Name}")
				: AssetDatabase.FindAssets($"t:{type.Name}", new string[] { path });
		}

		public static void Rename(UnityObject asset, string newName, bool setDirty = false)
		{
			string assetPath = AssetDatabase.GetAssetPath(asset);
			AssetDatabase.RenameAsset(assetPath, newName);

			if (setDirty)
			{
				EditorUtility.SetDirty(asset);
			}
		}

		public static bool Delete(UnityObject asset)
		{
			string assetPath = AssetDatabase.GetAssetPath(asset);
			return AssetDatabase.DeleteAsset(assetPath);
		}

		public static string GetGUID(UnityObject asset)
		{
			return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
		}

		public static bool TryGetAssetByGUID(string guid, out UnityObject asset)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);

			if (!path.IsNullOrEmpty())
			{
				asset = AssetDatabase.LoadAssetAtPath<UnityObject>(path);
				return true;
			}

			asset = null;
			return false;
		}

		public static string GetAbsolutePath(this string relativeAssetPath)
		{
			if (relativeAssetPath.StartsWith("Assets"))
				return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativeAssetPath));

			return Path.Combine(Application.dataPath, relativeAssetPath);
		}

		/// <summary>
		/// Создает папки, если они не существуют
		/// </summary>
		public static void EnsureOrCreateFolder(string path)
		{
			if (AssetDatabase.IsValidFolder(path))
				return;

			var split = path.Split('/');
			var current = split[0];
			for (int i = 1; i < split.Length; i++)
			{
				var next = $"{current}/{split[i]}";
				if (!AssetDatabase.IsValidFolder(next))
					AssetDatabase.CreateFolder(current, split[i]);
				current = next;
			}
		}

		public static string GetFolderPath(UnityEngine.Object obj)
		{
			string path = AssetDatabase.GetAssetPath(obj);
			if (path.IsNullOrEmpty())
			{
				path = "Assets";
			}
			else
			if (System.IO.Path.GetExtension(path) != "")
			{
				path = path.Replace(System.IO.Path.GetFileName(path), "");
			}

			return path;
		}
	}
}
