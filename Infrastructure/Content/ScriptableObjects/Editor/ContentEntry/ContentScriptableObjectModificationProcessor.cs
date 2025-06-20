using System.IO;
using Content.Editor;
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
	}
}
