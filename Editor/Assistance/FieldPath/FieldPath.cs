using System;
using System.Collections.Generic;
using Fusumity.Editor.Extensions;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Fusumity.Editor.Assistance
{
	[Serializable]
	public struct FieldPath
	{
		// Can be SO, Prefab or Scene
		public string assetGuid;

		// Optional. Only for Prefab or Scene asset
		public ulong prefabInstanceId;
		// Optional. Only for Prefab or Scene asset
		public ulong objectLocalId;

		public string propertyPath;

		public bool IsValid => !string.IsNullOrEmpty(assetGuid) && !string.IsNullOrEmpty(propertyPath);

		public static FieldPath Create(InspectorProperty property, out bool isValid)
		{
			var targetObject = (Object)property.SerializationRoot.ValueEntry.WeakValues[0];
			return Create(targetObject, property.Path, out isValid);
		}

		public static FieldPath Create(SerializedProperty property, out bool isValid)
		{
			var targetObject = property.serializedObject.targetObject;
			return Create(targetObject, property.propertyPath, out isValid);
		}

		public static FieldPath Create(Object targetObject, string propertyPath, out bool isValid)
		{
			var result = new FieldPath
			{
				propertyPath = propertyPath,
			};
			var assetPath = default(string);

			isValid = true;

			if (targetObject is Component component)
			{
				// We are on GameObject
				var prefabStage = PrefabStageUtility.GetPrefabStage(component.transform.root.gameObject);
				// prefabStage is `null` if we are in the Scene
				var isPrefab = prefabStage != null;

				if (isPrefab)
				{
					assetPath = prefabStage.assetPath;
				}
				else
				{
					// GameObject on the Scene
					assetPath = component.gameObject.scene.path;

					// INVALID if:
					// We are Playing
					// OR
					// We are part of the Prefab
					isValid = !Application.isPlaying && !PrefabUtility.IsPartOfPrefabInstance(targetObject);
				}

				component.GetUnityObjectSourceId(out var prefabInstanceId, out var componentLocalId);
				result.prefabInstanceId = prefabInstanceId;
				result.objectLocalId = componentLocalId;
			}
			else
			{
				assetPath = AssetDatabase.GetAssetPath(targetObject);
				result.objectLocalId = targetObject.GetUnityObjectLocalId();
			}

			result.assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
			return result;
		}

		private static List<Component> _componentsBuffer = new ();

		public bool TryFindProperty(out SerializedProperty property)
		{
			return TryFindProperty(null, out property);
		}

		public bool TryFindProperty(Dictionary<string, (Scene scene, bool shouldUnload)> pathToScene, out SerializedProperty property)
		{
			property = null;
			var path = AssetDatabase.GUIDToAssetPath(assetGuid);
			if (string.IsNullOrEmpty(path))
				return false;

			var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
			if (asset is SceneAsset)
			{
				Scene scene;
				var shouldCloseScene = false;
				if (pathToScene == null)
				{
					var activeScene = SceneManager.GetActiveScene();
					if (activeScene.path == path)
						scene = activeScene;
					else
					{
						scene = SceneManager.GetSceneByPath(path);
						shouldCloseScene = !scene.isLoaded;
						if (scene.isLoaded)
							scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
					}
				}
				else
				{
					if (pathToScene.TryGetValue(path, out var value))
						scene = value.scene;
					else
					{
						scene = SceneManager.GetSceneByPath(path);
						if (scene.isLoaded)
							pathToScene.Add(path, (scene, false));
						else
						{
							scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
							pathToScene.Add(path, (scene, true));
						}
					}
				}

				foreach (var root in scene.GetRootGameObjects())
				{
					CheckRoot(root, out var hasLocalId, out property);
					if (hasLocalId)
					{
						if (shouldCloseScene)
							EditorSceneManager.CloseScene(scene, true);
						return property != null;
					}
				}
				if (shouldCloseScene)
					EditorSceneManager.CloseScene(scene, true);
			}
			else if (asset is GameObject root)
			{
				CheckRoot(root, out var hasLocalId, out property);
				return hasLocalId && property != null;
			}
			else if (asset is ScriptableObject)
			{
				property = CheckPropertyPath(asset);
				return property != null;
			}

			return false;
		}

		private void CheckRoot(GameObject root, out bool hasLocalId, out SerializedProperty property)
		{
			hasLocalId = false;
			property = null;

			root.GetComponentsInChildren(_componentsBuffer);
			foreach (var component in _componentsBuffer)
			{
				component.GetUnityObjectSourceId(out var otherPrefabInstanceId, out var otherComponentLocalId);
				if (prefabInstanceId != otherPrefabInstanceId && objectLocalId != otherComponentLocalId)
					continue;

				hasLocalId = true;
				property = CheckPropertyPath(component);
				return;
			}
		}

		private SerializedProperty CheckPropertyPath(Object target)
		{
			var serializedComponent = new SerializedObject(target);
			return serializedComponent.FindProperty(propertyPath);
		}

		public static bool operator ==(FieldPath a, FieldPath b)
		{
			return a.propertyPath == b.propertyPath &&
					a.prefabInstanceId == b.prefabInstanceId &&
					a.objectLocalId == b.objectLocalId &&
					a.assetGuid == b.assetGuid;
		}

		public static bool operator !=(FieldPath a, FieldPath b)
		{
			return !(a == b);
		}

		public override string ToString()
		{
			return $"[{nameof(FieldPath)}: {nameof(assetGuid)}={assetGuid}, {nameof(prefabInstanceId)}={prefabInstanceId}, {nameof(objectLocalId)}={objectLocalId}, {nameof(propertyPath)}={propertyPath}]";
		}
	}
}
