using System;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	using UnityObject = UnityEngine.Object;

	internal static class AssetReserializeTool
	{
		private const string ASSETS_MENU = "Assets/";
		private const string TOOL_MENU = "Tools/Other/Reserialize/";

		[MenuItem(ASSETS_MENU + "Reserialize", true, priority = 399)]
		private static bool Validate() => Selection.activeObject;

		[MenuItem(ASSETS_MENU + "Reserialize", false, priority = 399)]
		private static void Reserialize()
		{
			if (!Selection.activeObject)
				return;

			var selected = Selection.activeObject;
			var selectedPath = AssetDatabase.GetAssetPath(selected);

			if (AssetDatabase.IsValidFolder(selectedPath))
			{
				ReserializeAssets(folderPath: selectedPath);
			}
			else
			{
				ReserializeAsset(selected);
			}
		}

		[MenuItem(TOOL_MENU + "All \ud83d\udc0c", priority = 101_000)]
		private static void ReserializeAll()
		{
			ReserializeAssets<UnityObject>();
		}

		[MenuItem(TOOL_MENU + "Prefabs", priority = 100_000)]
		private static void ReserializePrefabs()
		{
			ReserializeAssets<GameObject>();
		}

		[MenuItem(TOOL_MENU + "Scriptable Objects", priority = 100_001)]
		private static void ReserializeScriptableObjects()
		{
			ReserializeAssets<ScriptableObject>();
		}

		private static void ReserializeAssets<T>(string folderPath = "/Assets") where T : UnityObject
			=> ReserializeAssets(typeof(T), folderPath);

		private static void ReserializeAssets(Type type = null, string folderPath = "/Assets")
		{
			try
			{
				var filter = type != null ? $"t: {type.Name}" : string.Empty;
				var guids = AssetDatabase.FindAssets(filter,
					new[] {folderPath});

				for (int i = 0; i < guids.Length; i++)
				{
					var guid = guids[i];
					var assetPath = AssetDatabase.GUIDToAssetPath(guid);
					if (AssetDatabase.IsValidFolder(assetPath))
						continue;

					if (i % 100 == 0)
					{
						var progress = (float) i / guids.Length;
						EditorUtility.DisplayProgressBar("Reserialize Assets",
							$"Reserialize Assets... ({i}/{guids.Length}) Reserialized: {i}", progress);
					}

					var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
					if (!asset)
						continue;

					ReserializeAsset(asset, false);
				}

				AssetDatabase.SaveAssets();
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		private static void ReserializeAsset(UnityObject asset, bool save = true)
		{
			EditorUtility.SetDirty(asset);
			if (save)
				AssetDatabase.SaveAssetIfDirty(asset);
		}
	}
}
