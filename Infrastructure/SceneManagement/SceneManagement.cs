using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace SceneManagement
{
	public class SceneLoaderHub
	{
		private readonly Dictionary<string, SceneLoader> _nameToLoader = new();

		public void LoadScene(string sceneName, bool activateScene, Action<Scene> completeLoadCallback = null,
			Action interruptLoadingCallback = null, Action completeUnloadCallback = null)
		{
			var loader = GetOrCreateLoader(sceneName);
			loader.LoadScene(activateScene, completeLoadCallback, interruptLoadingCallback, completeUnloadCallback);
		}

		public void UnloadScene(string sceneName, Action completeUnloadCallback = null)
		{
			var loader = GetOrCreateLoader(sceneName);
			loader.UnloadScene(completeUnloadCallback);
		}

		public void ReloadScene(string sceneName, bool activateScene, Action<Scene> completeLoadCallback = null,
			Action interruptLoadingCallback = null, Action completeUnloadCallback = null)
		{
			var loader = GetOrCreateLoader(sceneName);
			loader.ReloadScene(activateScene, completeLoadCallback, interruptLoadingCallback, completeUnloadCallback);
		}

		private SceneLoader GetOrCreateLoader(string sceneName)
		{
			if (!_nameToLoader.TryGetValue(sceneName, out var loader))
			{
				loader = new SceneLoader(sceneName);
				_nameToLoader.Add(sceneName, loader);
			}

			return loader;
		}
	}
}
