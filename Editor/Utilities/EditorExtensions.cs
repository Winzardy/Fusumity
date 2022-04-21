using System;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Utilities
{
	public static class EditorExtensions
	{
		public static bool IsStandardType(this SerializedProperty property)
		{
			return property.propertyType != SerializedPropertyType.Generic & property.propertyType != SerializedPropertyType.ManagedReference;
		}

		public static string GetParentPropertyPath(this SerializedProperty property)
		{
			return ReflectionExtensions.GetParentPath(property.propertyPath);
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
			return ReflectionExtensions.GetObjectByLocalPath(serializedObject.targetObject, objectPath);
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
			ReflectionExtensions.SetObjectByLocalPath(serializedObject.targetObject, propertyPath, value);
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

			ReflectionExtensions.InvokeMethodByLocalPath(property.serializedObject.targetObject, fullMethodPath);
		}

		public static void DrawBody(this SerializedProperty property, Rect position)
		{
			if (property.IsStandardType())
			{
				EditorGUI.PropertyField(position, property, new GUIContent(" "), true);
				return;
			}
			if (!property.hasVisibleChildren)
			{
				EditorGUI.PropertyField(position, property, new GUIContent(" "), false);
				return;
			}
			var currentProperty = property.serializedObject.FindProperty(property.propertyPath);

			currentProperty.NextVisible(true);
			do
			{
				if (!currentProperty.propertyPath.StartsWith(property.propertyPath + ReflectionExtensions.pathSplitChar))
				{
					break;
				}

				var height = EditorGUI.GetPropertyHeight(currentProperty, true);
				position.height = height;
				EditorGUI.PropertyField(position, currentProperty, true);
				position.y += height;
			} while (currentProperty.NextVisible(false));

			currentProperty.Dispose();
		}

		public static float GetBodyHeight(this SerializedProperty property)
		{
			if (property.propertyType != SerializedPropertyType.ManagedReference)
				return EditorGUI.GetPropertyHeight(property, true);

			var height = EditorGUI.GetPropertyHeight(property, false);
			if (!property.hasVisibleChildren)
			{
				return height;
			}
			var currentProperty = property.serializedObject.FindProperty(property.propertyPath);

			currentProperty.NextVisible(true);
			do
			{
				if (!currentProperty.propertyPath.StartsWith(property.propertyPath + ReflectionExtensions.pathSplitChar))
				{
					break;
				}

				height += currentProperty.GetBodyHeight();
			}
			while (currentProperty.NextVisible(false));
			currentProperty.Dispose();

			return height;
		}
	}
}
