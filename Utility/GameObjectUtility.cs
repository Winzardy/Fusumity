using System.Collections.Generic;
using Sapientia.Collections;
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
