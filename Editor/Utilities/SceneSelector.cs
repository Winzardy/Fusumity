using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Fusumity.Editor.Utilities
{
	[InitializeOnLoad]
	public static class SceneSelector
	{
		private static ScriptableObject _toolbar;
		private static string[]         _scenePaths;
		private static string[]         _sceneNames;

		static SceneSelector()
		{
			EditorApplication.update -= Update;
			EditorApplication.update += Update;
		}

		private static void Update()
		{
			if (_toolbar == null)
			{
				var editorAssembly = typeof(UnityEditor.Editor).Assembly;

				var toolbars = Resources.FindObjectsOfTypeAll(editorAssembly.GetType("UnityEditor.Toolbar"));
				_toolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
				if (_toolbar != null)
				{
#if UNITY_2020_1_OR_NEWER
					var windowBackendPropertyInfo = editorAssembly.GetType("UnityEditor.GUIView").GetProperty("windowBackend", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					var windowBackend = windowBackendPropertyInfo.GetValue(_toolbar);
					var visualTreePropertyInfo = windowBackend.GetType().GetProperty("visualTree", BindingFlags.Public | BindingFlags.Instance);
					var visualTree = (VisualElement)visualTreePropertyInfo.GetValue(windowBackend);
#else
				PropertyInfo  visualTreePropertyInfo = editorAssembly.GetType("UnityEditor.GUIView").GetProperty("visualTree", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				VisualElement visualTree             = (VisualElement)visualTreePropertyInfo.GetValue(_toolbar, null);
#endif

					var container = (IMGUIContainer)visualTree[0];

					var onGUIHandlerFieldInfo = typeof(IMGUIContainer).GetField("m_OnGUIHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					var handler = (Action)onGUIHandlerFieldInfo.GetValue(container);
					handler -= OnGUI;
					handler += OnGUI;
					onGUIHandlerFieldInfo.SetValue(container, handler);
				}
			}

			if (_scenePaths == null || _scenePaths.Length != EditorBuildSettings.scenes.Length)
			{
				var scenePaths = new List<string>();
				var sceneNames = new List<string>();

				foreach (var scene in EditorBuildSettings.scenes)
				{
					if (scene.path == null || scene.path.StartsWith("Assets") == false)
						continue;

					var scenePath = Application.dataPath + scene.path.Substring(6);

					scenePaths.Add(scenePath);
					sceneNames.Add(Path.GetFileNameWithoutExtension(scenePath));
				}

				_scenePaths = scenePaths.ToArray();
				_sceneNames = sceneNames.ToArray();
			}
		}

		private static void OnGUI()
		{
			using (new EditorGUI.DisabledScope(Application.isPlaying))
			{
				var rect = new Rect(0, 0, Screen.width, Screen.height)
				{
					xMin = EditorGUIUtility.currentViewWidth * 0.5f + 100.0f,
					xMax = EditorGUIUtility.currentViewWidth - 350.0f,
					y = 8.0f,
				};

				using (new GUILayout.AreaScope(rect))
				{
					var activeScene = SceneManager.GetActiveScene();
					var sceneName = activeScene.name;
					var sceneIndex = -1;

					for (var i = 0; i < _sceneNames.Length; ++i)
					{
						if (sceneName == _sceneNames[i])
						{
							sceneIndex = i;
							break;
						}
					}

					var newSceneIndex = EditorGUILayout.Popup(sceneIndex, _sceneNames, GUILayout.Width(200.0f));
					if (newSceneIndex != sceneIndex)
					{
						if (activeScene.isDirty)
						{
							var dialogResult = EditorUtility.DisplayDialogComplex("Scene Have Been Modified",
								"Do you want to save the changes you made in the scene:"
								+ $"\n{activeScene.path}"
								+ "\nYour changes will be lost if you don't save them.",
								"Save", "Cancel", "Don't Save");

							switch (dialogResult)
							{
								case 0: // Save
									EditorSceneManager.SaveScene(activeScene);
									break;
								case 1: // Cancel
									return;
								case 2: // Don't Save
									break;
							}
						}

						EditorSceneManager.OpenScene(_scenePaths[newSceneIndex], OpenSceneMode.Single);
					}
				}
			}
		}
	}
}
