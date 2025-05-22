using UnityEditor;
using UnityEngine;
using Fusumity.Editor.Utility;

namespace Fusumity.Editor
{
	public static class MissingReferenceComponentTool
	{
		private const int PRIORITY = 100;

		[MenuItem("GameObject/Find Missing Reference Component", true, priority = PRIORITY)]
		private static bool Validate()
		{
			if (!Selection.activeObject)
				return false;

			if (Selection.activeObject is not GameObject)
				return false;

			return true;
		}

		[MenuItem("GameObject/Find Missing Reference Component", false, priority = PRIORITY)]
		private static void Execute()
		{
			if (!Selection.activeObject)
				return;

			if (Selection.activeObject is not GameObject gameObject)
				return;

			if (!Find(gameObject))
				Debug.Log("Not found missing reference components");
		}

		[MenuItem("Tools/Other/GameObject/Find Missing Reference Component", false, priority = PRIORITY)]
		private static void FindEverywhere()
		{
			var count = 0;
			foreach (var obj in AssetDatabaseUtility.GetAssets(typeof(GameObject)))
			{
				if (Find((GameObject) obj))
					count++;
			}

			if (count == 0)
				Debug.Log("Not found missing reference components");
		}

		private static bool Find(GameObject gameObject)
		{
			var components = gameObject.GetComponentsInChildren<Component>(true);
			Component previous = null;
			var count = 0;
			foreach (var component in components)
			{
				if (component)
				{
					previous = component;
					continue;
				}

				count++;
				Debug.LogError($"[ {previous?.name ?? "..."} ] Missing Reference Component!", previous);
			}

			return count > 0;
		}
	}
}
