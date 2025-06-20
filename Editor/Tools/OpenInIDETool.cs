using System.Diagnostics;
using System.IO;
using UnityEditor;

namespace Fusumity.Editor
{
	using Debug = UnityEngine.Debug;

	public static class OpenInIdeTool
	{
		private const int PRIORITY = -100;

		[MenuItem("Assets/Open in IDE", true, priority = PRIORITY)]
		private static bool Validate()
		{
			if (!Selection.activeObject)
				return false;

			var path = AssetDatabase.GetAssetPath(Selection.activeObject);
			return !AssetDatabase.IsValidFolder(path);
		}

		[MenuItem("Assets/Open in IDE", false, priority = PRIORITY)]
		private static void Execute()
		{
			if (!Selection.activeObject)
				return;

			if (Selection.activeObject is MonoScript script)
			{
				AssetDatabase.OpenAsset(script);
				return;
			}

			var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			var fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
			var editorPath = EditorPrefs.GetString("kScriptsDefaultApp");

			if (string.IsNullOrEmpty(editorPath))
			{
				Debug.LogError("Not found editor path!");
				return;
			}

#if UNITY_EDITOR_OSX
			var supported = false;
			if (editorPath.Contains("Rider"))
			{
				supported = true;
				editorPath += "/Contents/MacOS/rider";
			}

			if (editorPath.Contains("Visual Studio.app"))
			{
				supported = true;
				editorPath += "/Contents/MacOS/VisualStudio";
			}

			if (editorPath.Contains("Visual Studio Code.app"))
			{
				supported = true;
				editorPath += "/Contents/Resources/app/bin/code";
			}

			if (!supported)
			{
				Debug.LogError("Not supported IDE!");
				return;
			}
#endif

			Process.Start(editorPath, fullPath);
		}
	}
}
