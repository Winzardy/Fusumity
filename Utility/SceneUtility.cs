using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fusumity.Utility
{
	public static class SceneUtility
	{
		public static void MoveObjectTo(GameObject gameObject, string sceneName)
		{
			var scene = SceneManager.GetSceneByName(sceneName);
			if (scene.IsValid())
			{
				SceneManager.MoveGameObjectToScene(gameObject, scene);
			}
			else
			{
				Debug.LogError(
					$"Could not find valid scene [ {sceneName} ] " +
					$"to move gameObject [ {gameObject.name} ]", gameObject);
			}
		}
	}
}
