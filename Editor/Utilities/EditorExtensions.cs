using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Utilities
{
	public static class EditorExtensions
	{
		private const char _pathSplitChar = '.';

		private static readonly Dictionary<Type, Type[]> _assignableFrom = new Dictionary<Type, Type[]>();
		private static readonly Dictionary<Type, Type[]> _typesWithNull = new Dictionary<Type, Type[]>();
		private static readonly Dictionary<Type, Type[]> _typesWithoutNull = new Dictionary<Type, Type[]>();

		public static Type[] GetInheritorTypes(this Type baseType, bool insertNull = false)
		{
			Type[] inheritorTypes;
			if (insertNull)
			{
				if (_typesWithNull.TryGetValue(baseType, out inheritorTypes))
					return inheritorTypes;
			}
			else if (_typesWithoutNull.TryGetValue(baseType, out inheritorTypes))
				return inheritorTypes;

			if (!_assignableFrom.TryGetValue(baseType, out var typeArray))
			{
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				var typeList = new List<Type>();
				for (int a = 0; a < assemblies.Length; a++)
				{
					var types = assemblies[a].GetTypes();
					for (int t = 0; t < types.Length; t++)
					{
						if (baseType.IsAssignableFrom(types[t]) && !types[t].IsInterface && !types[t].IsAbstract &&
						    !types[t].IsGenericType)
						{
							typeList.Add(types[t]);
						}
					}
				}

				typeArray = typeList.ToArray();
				_assignableFrom.Add(baseType, typeArray);
			}

			if (insertNull)
			{
				inheritorTypes = new Type[typeArray.Length + 1];
				Array.ConstrainedCopy(typeArray, 0, inheritorTypes, 1, typeArray.Length);
			}
			else
			{
				inheritorTypes = typeArray;
			}

			(insertNull ? _typesWithNull : _typesWithoutNull).Add(baseType, inheritorTypes);

			return inheritorTypes;
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

		public static object GetObjectByLocalPath(SerializedObject serializedObject, string objectPath, bool lastObjectIsNotArray = false)
		{
			var target = (object)serializedObject.targetObject;
			var lastNotArray = target;

			var pathComponents = objectPath.Split(_pathSplitChar);

			for (var p = 0; p < pathComponents.Length; p++)
			{
				var pathComponent = pathComponents[p];
				if (target is Array array)
				{
					if (p < pathComponents.Length - 1 && pathComponents[p + 1].StartsWith("data["))
					{
						var index = int.Parse(pathComponents[++p].Replace("data[", "").Replace("]", ""));
						target = array.GetValue(index);
					}
				}
				else
				{
					lastNotArray = target;
					var field = GetAnyField(target.GetType(), pathComponent);
					target = field.GetValue(target);
				}
			}

			if (lastObjectIsNotArray && target is Array)
			{
				target = lastNotArray;
			}

			return target;
		}

		public static Type GetPropertyTypeByLocalPath(SerializedObject serializedObject, string propertyPath)
		{
			return GetObjectByLocalPath(serializedObject, propertyPath).GetType();
		}

		public static Type GetPropertyTypeByLocalPath(this SerializedProperty property)
		{
			return GetPropertyTypeByLocalPath(property.serializedObject, property.propertyPath);
		}

		public static FieldInfo GetAnyField(this Type type, string fieldName)
		{
			var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			while (field == null)
			{
				type = type.BaseType;
				field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			}

			return field;
		}

		public static MethodInfo GetAnyMethod(this Type type, string methodName)
		{
			var methodInfo = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			while (methodInfo == null)
			{
				type = type.BaseType;
				methodInfo = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
			}

			return methodInfo;
		}

		public static string GetPropertyParentPath(this SerializedProperty property)
		{
			var propertyPath = property.propertyPath;
			var removeIndex = propertyPath.LastIndexOf(_pathSplitChar);
			if (removeIndex >= 0)
				propertyPath = propertyPath.Remove(removeIndex, propertyPath.Length - removeIndex);

			return propertyPath;
		}

		public static SerializedProperty GetParent(this SerializedProperty property)
		{
			var parentPath = property.GetPropertyParentPath();
			var parent = property.serializedObject.FindProperty(parentPath);

			return parent;
		}

		public static void InvokeMethodByLocalPath(this SerializedProperty property, string methodPath)
		{
			var propertyPath = property.GetPropertyParentPath();

			var removeIndex = methodPath.LastIndexOf(_pathSplitChar);
			var methodName = methodPath;

			if (removeIndex > 0)
			{
				propertyPath += methodPath.Remove(removeIndex, methodPath.Length - removeIndex);
				methodName = methodPath.Remove(0, removeIndex + 1);
			}

			var target = GetObjectByLocalPath(property.serializedObject, propertyPath, true);
			var methodInfo = target.GetType().GetAnyMethod(methodName);

			methodInfo.Invoke(target, null);
		}

		public static SerializedProperty GetPropertyByLocalPath(this SerializedProperty property, string path)
		{
			var parentPath = property.GetPropertyParentPath();
			var fullPath = parentPath + '.' + path;

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
