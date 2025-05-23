using Content.Editor;
using UnityEditor;

namespace Content.ScriptableObjects.Editor
{
	public class ContentScriptableObjectModificationProcessor : AssetModificationProcessor
	{
		private static string[] OnWillSaveAssets(string[] paths)
		{
			foreach (var path in paths)
			{
				var asset = AssetDatabase.LoadAssetAtPath<ContentScriptableObject>(path);
				if (!asset)
					continue;

				var origin = ContentDebug.Logging.Nested.refresh;
				ContentDebug.Logging.Nested.refresh = false;
				asset.Refresh();
				ContentDebug.Logging.Nested.refresh = origin;
			}

			return paths;
		}
	}
}
