using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace SceneManagement
{
	public class SceneLoaderHub
	{
		public const string SUB_SCENE_POSTFIX = SceneLoader.SUB_SCENE_POSTFIX;
		public const string EMPTY_SCENE_POSTFIX = SceneLoader.EMPTY_SCENE_POSTFIX;

		private Dictionary<string, SceneLoader> _nameToLoader = new Dictionary<string, SceneLoader>();

		private SceneLoader GetOrCreateLoader(string sceneName)
		{
			if (!_nameToLoader.TryGetValue(sceneName, out var loader))
			{
				loader = new SceneLoader(sceneName);
				_nameToLoader.Add(sceneName, loader);
			}

			return loader;
		}

		public void ReloadSceneAsync(string sceneName, bool activateScene, Action<Scene> completeLoadCallback = null, Action interruptLoadingCallback = null, Action completeUnloadCallback = null)
		{
			var loader = GetOrCreateLoader(sceneName);
			loader.ReloadSceneAsync(activateScene, completeLoadCallback, interruptLoadingCallback, completeUnloadCallback);
		}

		public void LoadSceneAsync(string sceneName, bool activateScene, Action<Scene> completeLoadCallback = null, Action interruptLoadingCallback = null, Action completeUnloadCallback = null)
		{
			var loader = GetOrCreateLoader(sceneName);
			loader.LoadSceneAsync(activateScene, completeLoadCallback, interruptLoadingCallback, completeUnloadCallback);
		}

		public void UnloadSceneAsync(string sceneName, Action completeUnloadCallback = null)
		{
			var loader = GetOrCreateLoader(sceneName);
			loader.UnloadSceneAsync(completeUnloadCallback);
		}
	}
}
