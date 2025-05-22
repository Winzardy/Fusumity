using System.Collections.Generic;
using Sapientia.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fusumity.Utility
{
	public static class GameObjectUtility
	{
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
	}
}
