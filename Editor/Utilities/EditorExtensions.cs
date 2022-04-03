using System;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Utilities
{
	public static class EditorExtensions
	{
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

		public static object GetObjectByLocalPath(SerializedObject serializedObject, string objectPath)
		{
			return ReflectionExtensions.GetObjectByLocalPath(serializedObject.targetObject, objectPath);
		}

		public static Type GetPropertyTypeByLocalPath(SerializedObject serializedObject, string propertyPath)
		{
			return GetObjectByLocalPath(serializedObject, propertyPath).GetType();
		}

		public static Type GetPropertyTypeByLocalPath(this SerializedProperty property)
		{
			return GetPropertyTypeByLocalPath(property.serializedObject, property.propertyPath);
		}

		public static string GetPropertyParentPath(this SerializedProperty property)
		{
			return ReflectionExtensions.GetParentPath(property.propertyPath);
		}

		public static SerializedProperty GetParentProperty(this SerializedProperty property)
		{
			var parentPath = property.GetPropertyParentPath();
			var parent = property.serializedObject.FindProperty(parentPath);

			return parent;
		}

		public static void InvokeMethodByLocalPath(this SerializedProperty property, string methodPath)
		{
			var parentPath = property.GetPropertyParentPath();
			var fullMethodPath = parentPath.AppendPath(methodPath);

			ReflectionExtensions.InvokeMethodByLocalPath(property.serializedObject.targetObject, fullMethodPath);
		}

		public static SerializedProperty GetPropertyByLocalPath(this SerializedProperty property, string localPath)
		{
			var parentPath = property.GetPropertyParentPath();
			var fullPath = parentPath.AppendPath(localPath);

			return property.serializedObject.FindProperty(fullPath);
		}

		public static void DrawBody(this SerializedProperty property, Rect position)
		{
			if (!property.hasVisibleChildren)
			{
				EditorGUI.PropertyField(position, property, new GUIContent(" "), false);
				return;
			}
			var currentProperty = property.serializedObject.FindProperty(property.propertyPath);

			currentProperty.NextVisible(true);
			do
			{
				if (currentProperty.propertyPath.StartsWith(property.propertyPath + ".") == false)
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
	}
}
