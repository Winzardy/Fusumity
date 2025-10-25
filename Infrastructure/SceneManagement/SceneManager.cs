using System;
using System.Runtime.CompilerServices;
using Sapientia;
using UnityEngine.SceneManagement;

namespace SceneManagement
{
	public class SceneManager : StaticProvider<SceneLoaderHub>
	{
		public const string SUB_SCENE_POSTFIX = SceneLoader.SUB_SCENE_POSTFIX;
		public const string EMPTY_SCENE_POSTFIX = SceneLoader.EMPTY_SCENE_POSTFIX;

		private static SceneLoaderHub hub
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		public static void LoadScene(string sceneName, bool activateScene, Action<Scene> completeLoadCallback = null,
			Action interruptLoadingCallback = null, Action completeUnloadCallback = null) =>
			hub.LoadScene(sceneName, activateScene, completeLoadCallback, interruptLoadingCallback, completeUnloadCallback);

		public static void UnloadScene(string sceneName, Action completeUnloadCallback = null) =>
			hub.UnloadScene(sceneName, completeUnloadCallback);

		public static void ReloadScene(string sceneName, bool activateScene, bool additive = false, bool empty = false,
			Action<Scene> completeLoadCallback = null,
			Action interruptLoadingCallback = null, Action completeUnloadCallback = null) =>
			hub.ReloadScene(sceneName, activateScene, completeLoadCallback, interruptLoadingCallback, completeUnloadCallback);
	}
}
