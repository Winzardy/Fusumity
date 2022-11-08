using System;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Extensions
{
	public static class EditorExt
	{
		private static Rect positionCache;
		private static GUIContent labelCache;
		private static bool includeChildrenCache;

		public static bool IsStandardType(this SerializedProperty property)
		{
			return property.propertyType != SerializedPropertyType.Generic & property.propertyType != SerializedPropertyType.ManagedReference;
		}

		public static string GetParentPropertyPath(this SerializedProperty property)
		{
			return ReflectionExt.GetParentPath(property.propertyPath);
		}

		public static Type GetManagedReferenceType(this SerializedProperty property)
		{
			var typeName = property.managedReferenceFullTypename;

			var parts = typeName.Split(' ');
			if (parts.Length == 2)
			{
				var assemblyPart = parts[0];
				var nsClassnamePart = parts[1];
				return Type.GetType($"{nsClassnamePart}, {assemblyPart}");
			}

			return null;
		}

		public static object GetObjectByPath(SerializedObject serializedObject, string objectPath)
		{
			return ReflectionExt.GetObjectByLocalPath(serializedObject.targetObject, objectPath);
		}

		public static Type GetPropertyTypeByPath(SerializedObject serializedObject, string propertyPath)
		{
			return GetObjectByPath(serializedObject, propertyPath)?.GetType();
		}

		public static Type GetPropertyType(this SerializedProperty property)
		{
			return GetPropertyTypeByPath(property.serializedObject, property.propertyPath);
		}

		public static Type GetPropertyTypeByLocalPath(this SerializedProperty property, string localPath)
		{
			return GetPropertyTypeByPath(property.serializedObject, property.propertyPath.AppendPath(localPath));
		}

		public static void SetPropertyValueByLocalPath(SerializedObject serializedObject, string propertyPath, object value)
		{
			ReflectionExt.SetObjectByLocalPath(serializedObject.targetObject, propertyPath, value);
		}

		public static void SetPropertyValue(this SerializedProperty property, object value)
		{
			SetPropertyValueByLocalPath(property.serializedObject, property.propertyPath, value);
		}

		public static SerializedProperty GetParentProperty(this SerializedProperty property)
		{
			var parentPath = property.GetParentPropertyPath();
			var parent = property.serializedObject.FindProperty(parentPath);

			return parent;
		}

		public static SerializedProperty GetPropertyByLocalPath(this SerializedProperty property, string localPath)
		{
			var parentPath = property.GetParentPropertyPath();
			var fullPath = parentPath.AppendPath(localPath);

			return property.serializedObject.FindProperty(fullPath);
		}

		public static void InvokeMethodByLocalPath(this SerializedProperty property, string methodPath)
		{
			var parentPath = property.GetParentPropertyPath();
			var fullMethodPath = parentPath.AppendPath(methodPath);

			ReflectionExt.InvokeMethodByLocalPath(property.serializedObject.targetObject, fullMethodPath);
		}

		public static void DrawBody(this SerializedProperty property, Rect position)
		{
			if (property.IsStandardType())
			{
				property.PropertyField(position, new GUIContent(" "), true);
				return;
			}
			if (!property.hasVisibleChildren)
			{
				property.PropertyField(position, new GUIContent(" "), false);
				return;
			}
			var currentProperty = property.serializedObject.FindProperty(property.propertyPath);

			currentProperty.NextVisible(true);
			do
			{
				if (!currentProperty.propertyPath.StartsWith(property.propertyPath + ReflectionExt.pathSplitChar))
				{
					break;
				}

				var height = currentProperty.GetPropertyHeight(true);
				position.height = height;
				currentProperty.PropertyField(position, includeChildren: true);
				position.y += height;
			} while (currentProperty.NextVisible(false));

			currentProperty.Dispose();
		}

		public static float GetBodyHeight(this SerializedProperty property)
		{
			if (property.propertyType != SerializedPropertyType.ManagedReference)
				return property.GetPropertyHeight(true);

			var height = property.GetPropertyHeight(false);
			if (!property.hasVisibleChildren)
			{
				return height;
			}
			var currentProperty = property.serializedObject.FindProperty(property.propertyPath);

			currentProperty.NextVisible(true);
			do
			{
				if (!currentProperty.propertyPath.StartsWith(property.propertyPath + ReflectionExt.pathSplitChar))
				{
					break;
				}

				height += currentProperty.GetBodyHeight();
			}
			while (currentProperty.NextVisible(false));
			currentProperty.Dispose();

			return height;
		}

		public static void PropertyField(this SerializedProperty property, Rect position, GUIContent label = null, bool includeChildren = true)
		{
			positionCache = position;
			labelCache = label;
			includeChildrenCache = includeChildren;

			property.PropertyField_Cached();
		}

		public static float GetPropertyHeight(this SerializedProperty property, bool includeChildren)
		{
			includeChildrenCache = includeChildren;

			return property.GetPropertyHeight_Cached();
		}

		public static void PropertyField_Cached(this SerializedProperty property)
		{
			EditorGUI.PropertyField(positionCache, property, labelCache, includeChildrenCache);
		}

		public static float GetPropertyHeight_Cached(this SerializedProperty property)
		{
			return EditorGUI.GetPropertyHeight(property, includeChildrenCache);;
		}
	}
}