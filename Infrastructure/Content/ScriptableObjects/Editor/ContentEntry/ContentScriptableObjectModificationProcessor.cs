using System.Collections.Generic;
using System.IO;
using Content.Editor;
using Sapientia.Extensions;
using UnityEditor;

namespace Content.ScriptableObjects.Editor
{
	public class ContentScriptableObjectModificationProcessor : AssetModificationProcessor
	{
		private const string ASSET_EXTENSION = ".asset";

		private static string[] OnWillSaveAssets(string[] paths)
		{
			foreach (var path in paths)
			{
				if (Path.GetExtension(path) != ASSET_EXTENSION)
					continue;

				var asset = AssetDatabase.LoadAssetAtPath<ContentScriptableObject>(path);
				if (!asset)
					continue;

				if (asset is ContentEntryScriptableObject entryScriptableObject)
					entryScriptableObject.Sync(false);

				var origin = ContentDebug.Logging.Nested.refresh;
				ContentDebug.Logging.Nested.refresh = false;
				asset.Refresh();
				ContentDebug.Logging.Nested.refresh = origin;
			}

			return paths;
		}

		private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
		{
			var asset = AssetDatabase.LoadAssetAtPath<ContentScriptableObject>(assetPath);

			if (asset != null)
			{
				ContentDatabaseEditorUtility.RemoveToDatabase(asset);

				ContentAutoConstantsGenerator.ForceInvokeWithDelay(asset.GetType());
			}

			return AssetDeleteResult.DidNotDelete;
		}

		private static void OnWillCreateAsset(string path)
		{
			AddPendingAsset(path);
		}

		private static readonly HashSet<string> _pendingAssetPaths = new();

		private static void AddPendingAsset(string path)
		{
			path = path.Remove(".meta");
			_pendingAssetPaths.Add(path);
		}

		internal static bool RemovePendingAsset(string path) => _pendingAssetPaths.Remove(path);
	}

	public class ContentPostprocessor : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths)
		{
			foreach (var path in importedAssets)
			{
				var asset = AssetDatabase.LoadAssetAtPath<ContentScriptableObject>(path);

				if (!asset)
					continue;

				if (ContentScriptableObjectModificationProcessor.RemovePendingAsset(path))
				{
					if (asset.NeedSync())
						asset.Sync(true);
				}

				if (asset.Enabled)
					ContentDatabaseEditorUtility.AddToDatabase(asset);
				else
					ContentDatabaseEditorUtility.RemoveToDatabase(asset);
			}

			foreach (var path in deletedAssets)
			{
				var asset = AssetDatabase.LoadAssetAtPath<ContentScriptableObject>(path);

				if (!asset)
					continue;

				ContentDatabaseEditorUtility.RemoveToDatabase(asset);
			}
		}
	}
}
