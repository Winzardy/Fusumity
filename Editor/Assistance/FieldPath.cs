using System;
using Fusumity.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Assistance
{
	[Serializable]
	public struct FieldPath
	{
		// Can be SO, Prefab or Scene
		public string assetGuid;
		// For a Prefab or a Scene Object
		public ulong componentLocalId;
		public string propertyPath;

		public static FieldPath Create(SerializedProperty property)
		{
			var targetObject = property.serializedObject.targetObject;

			var componentLocalId = default(ulong);
			var assetPath = default(string);

			if (targetObject is Component component)
			{
				var isPrefab = PrefabUtility.IsPartOfPrefabAsset(targetObject);

				if (isPrefab)
				{
					targetObject = component.transform.root.gameObject;
					assetPath = AssetDatabase.GetAssetPath(targetObject);
				}
				else
					assetPath = component.gameObject.scene.path;

				componentLocalId = component.GetComponentLocalId();
			}
			else
			{
				assetPath = AssetDatabase.GetAssetPath(targetObject);
			}

			return new FieldPath
			{
				assetGuid = AssetDatabase.AssetPathToGUID(assetPath),
				propertyPath = property.propertyPath,
				componentLocalId = componentLocalId,
			};
		}
	}
}