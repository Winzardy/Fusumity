using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fusumity.Utility
{
	public class SceneHolder : IDisposable
	{
		private string _name;

		private bool _created;
		private Scene _scene;

		public SceneHolder(string name)
		{
			_name = name;
		}

		public void Dispose()
		{
			if (_created)
				SceneManager.UnloadSceneAsync(_scene);
		}

		public void MoveToHolder(GameObject go)
		{
			TryCreateScene();
			SceneManager.MoveGameObjectToScene(go, _scene);
		}

		private void TryCreateScene()
		{
			if (_created)
				return;

			var existingScene = SceneManager.GetSceneByName(_name);

			_scene = existingScene.IsValid() ?
				existingScene :
				SceneManager.CreateScene(_name);

			_created = true;
		}
	}

	public static class SceneHolderExtensions
	{
		public static void MoveTo<T>(this T component, SceneHolder holder)
			where T : Component
		{
			component.gameObject.MoveTo(holder);
		}

		public static void MoveTo(this GameObject go, SceneHolder holder)
		{
			holder.MoveToHolder(go);
		}
	}
}
