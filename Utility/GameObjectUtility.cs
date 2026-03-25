using Sapientia.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fusumity.Utility
{
	public static class GameObjectUtility
	{
		public static DisableGameObjectScope BeginDisableScope(this GameObject gameObject)
		{
			return new DisableGameObjectScope(gameObject);
		}

		public static void SetActive(this IEnumerable<GameObject> gameObjects, bool active)
		{
			foreach (var gameObject in gameObjects)
			{
				gameObject.SetActive(active);
			}
		}

		public static void SetActiveSafe(this GameObject[] gameObjects, bool active)
		{
			if (gameObjects.IsNullOrEmpty())
				return;

			foreach (var gameObject in gameObjects)
			{
				gameObject.SetActive(active);
			}
		}

		public static void MoveToScene(this GameObject go, Scene scene)
		{
			SceneManager.MoveGameObjectToScene(go, scene);
		}

		public static bool IsActive(this GameObject go)
			=> go.activeSelf && go.activeInHierarchy;

		public static void ForceSaveEditor(this Object obj)
		{
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(obj);
			UnityEditor.AssetDatabase.SaveAssetIfDirty(obj);
#endif
		}

		public static void SetLayerRecursively(this GameObject obj, int layer)
		{
			obj.layer = layer;

			var childCount = obj.transform.childCount;
			for (var i = 0; i < childCount; i++)
			{
				SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
			}
		}

		/// <summary>
		/// Negative depth - represents objects higher in the hierarchy chain (parents, clamps to root).
		/// Positive depth - objects cascading down the hierarchy (1 - first child, 2 - first child's first child etc., clamps to deepest available).
		/// Depth 0 - object itself.
		/// </summary>
		public static GameObject GetAtDepth(this GameObject start, int depth)
		{
			var current = start.transform;

			if (depth < 0)
			{
				for (int i = 0; i < -depth; i++)
				{
					if (current.parent == null)
						break;

					current = current.parent;
				}
			}
			else if (depth > 0)
			{
				for (int i = 0; i < depth; i++)
				{
					if (current.childCount == 0)
						break;

					current = current.GetChild(0);
				}
			}

			return current.gameObject;
		}

		public readonly ref struct DisableGameObjectScope
		{
			public readonly GameObject gameObject;
			public readonly bool isActive;

			public DisableGameObjectScope(GameObject gameObject)
			{
				this.gameObject = gameObject;
				isActive = gameObject.activeSelf;

				if (isActive)
					gameObject.SetActive(false);
			}

			public void Dispose()
			{
				if (isActive)
					gameObject.SetActive(true);
			}
		}
	}
}
