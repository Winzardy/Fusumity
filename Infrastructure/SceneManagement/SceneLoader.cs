using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneManagement
{
	using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

	internal class SceneLoader
	{
		/// <summary>
		/// GameSceneState is used to track the state of the scene loading.
		/// </summary>
		public enum GameSceneState
		{
			Unloaded,
			Unloading,
			Loaded,
			Loading,
		}

		public const string SUB_SCENE_POSTFIX = "SubScene";
		public const string EMPTY_SCENE_POSTFIX = "Empty" + SUB_SCENE_POSTFIX;

		public readonly string sceneName;
		public readonly LoadSceneMode loadSceneMode;
		public readonly bool isEmptyScene;

		private Scene _scene;
		private GameSceneState _currentState = GameSceneState.Unloaded;
		private bool _shouldInterruptLoading;
		private bool _activateScene;

		private event Action<Scene> CompleteLoadCallback;
		private event Action InterruptLoadingCallback;
		private event Action CompleteUnloadCallback;

		public SceneLoader(string sceneName)
		{
			this.sceneName = sceneName;
			loadSceneMode = sceneName.Contains(SUB_SCENE_POSTFIX) ? LoadSceneMode.Additive : LoadSceneMode.Single;
			isEmptyScene = sceneName.Contains(EMPTY_SCENE_POSTFIX);

			if (!isEmptyScene)
				_scene = UnitySceneManager.GetSceneByName(sceneName);
		}

		public void ReloadSceneAsync(bool activateScene, Action<Scene> completeLoadCallback = null, Action interruptLoadingCallback = null, Action completeUnloadCallback = null)
		{
			UnloadSceneAsync(() =>
			{
				LoadSceneAsync(activateScene, completeLoadCallback, interruptLoadingCallback);
			});
		}

		public void LoadSceneAsync(bool activateScene, Action<Scene> completeLoadCallback = null, Action interruptLoadingCallback = null, Action completeUnloadCallback = null)
		{
			_activateScene = activateScene;
			CompleteLoadCallback += completeLoadCallback;

			switch (_currentState)
			{
				case GameSceneState.Loaded:
					CompleteLoadCallback?.Invoke(_scene);
					CompleteLoadCallback = null;
					CompleteUnloadCallback += completeUnloadCallback;
					return;
				case GameSceneState.Loading:
					InterruptLoadingCallback += interruptLoadingCallback;
					CompleteUnloadCallback += completeUnloadCallback;
					return;
				case GameSceneState.Unloading:
					CompleteUnloadCallback = () => LoadSceneAsync(activateScene, completeLoadCallback, interruptLoadingCallback, completeUnloadCallback);
					return;
			}

			CompleteUnloadCallback += completeUnloadCallback;
			InterruptLoadingCallback = interruptLoadingCallback;

			if (_scene.isLoaded)
			{
				CompleteLoad();
				return;
			}

			_currentState = GameSceneState.Loading;

			if (isEmptyScene)
			{
				_scene = UnitySceneManager.CreateScene(sceneName, new CreateSceneParameters
				{
					localPhysicsMode = LocalPhysicsMode.None
				});
				CompleteLoad();
			}
			else
			{
				var asyncOperation = UnitySceneManager.LoadSceneAsync(_scene.buildIndex, loadSceneMode);
				Debug.Assert(asyncOperation != null);

				asyncOperation.allowSceneActivation = true;
				asyncOperation.completed += CompleteLoad;
			}
		}

		public void UnloadSceneAsync(Action completeUnloadCallback = null)
		{
			CompleteUnloadCallback += completeUnloadCallback;

			switch (_currentState)
			{
				case GameSceneState.Loading:
					_shouldInterruptLoading = true;
					return;
				case GameSceneState.Unloading:
					return;
				case GameSceneState.Unloaded:
					CompleteUnload();
					return;
			}

			if (!_scene.isLoaded)
			{
				CompleteUnload();
				return;
			}

			_currentState = GameSceneState.Unloading;

			var asyncOperation = UnitySceneManager.UnloadSceneAsync(_scene);
			Debug.Assert(asyncOperation != null);

			asyncOperation.completed += CompleteUnload;
		}

		private void CompleteLoad(AsyncOperation _ = null)
		{
			if (_shouldInterruptLoading)
			{
				_currentState = GameSceneState.Loaded;
				UnloadSceneAsync();
				return;
			}
			InterruptLoadingCallback = null;

			if (_activateScene)
				UnitySceneManager.SetActiveScene(_scene);
			_currentState = GameSceneState.Loaded;

			var toInvoke = CompleteLoadCallback;
			CompleteLoadCallback = null;
			toInvoke?.Invoke(_scene);
		}

		private void CompleteUnload(AsyncOperation _ = null)
		{
			_currentState = GameSceneState.Unloaded;

			if (_shouldInterruptLoading)
			{
				var toInvoke = InterruptLoadingCallback;
				InterruptLoadingCallback = null;

				toInvoke?.Invoke();
				_shouldInterruptLoading = false;
			}
			{
				InterruptLoadingCallback = null;
				CompleteLoadCallback = null;
				var toInvoke = CompleteUnloadCallback;
				CompleteUnloadCallback = null;
				toInvoke?.Invoke();
			}
		}
	}
}
