using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Utilities
{
	public static class EditorExtensions
	{
		private const BindingFlags _InternalFieldBindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;

		private static readonly Type _customPropertyDrawerType = typeof(CustomPropertyDrawer);
		private const string _customPropertyDrawerField_Type = "m_Type";
		private const string _customPropertyDrawerField_UseForChildren = "m_UseForChildren";

		private static readonly Type _propertyDrawerType = typeof(PropertyDrawer);
		private const string _propertyDrawerField_Attribute = "m_Attribute";
		private const string _propertyDrawerField_FieldInfo = "m_FieldInfo";

		private static readonly Dictionary<Type, Type[]> _assignableFrom = new Dictionary<Type, Type[]>();
		private static readonly Dictionary<Type, Type[]> _typesWithNull = new Dictionary<Type, Type[]>();
		private static readonly Dictionary<Type, Type[]> _typesWithoutNull = new Dictionary<Type, Type[]>();

		public static Type[] GetInheritorTypesForSelection(this Type baseType, bool insertNull = false)
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

		public static Type GetPropertyType(this SerializedProperty property)
		{
			var currentType = property.serializedObject.targetObject.GetType();
			var pathComponents = property.propertyPath.Split('.');

			for (int index = 0; index < pathComponents.Length; ++index)
			{
				var pathComponent = pathComponents[index];
				if (pathComponent == "Array" && index < pathComponents.Length - 1 &&
				    pathComponents[index + 1].StartsWith("data["))
				{
					currentType = currentType.GetElementType();
					Debug.Assert(currentType != null);
					++index;
				}
				else
				{
					if (currentType.IsInterface || currentType.IsAbstract)
					{
						var path = "";
						for (var i = 0; i < index - 1; i++)
						{
							path += pathComponents[i] + '.';
						}

						path += pathComponents[index - 1];
						var currentProperty = property.serializedObject.FindProperty(path);
						var typename = currentProperty.managedReferenceFullTypename;

						var assemblyAndName = typename.Split(' ');
						var assembly = assemblyAndName[0];
						var name = assemblyAndName[1];

						currentType = Type.GetType($"{name}, {assembly}");
					}

					var field = currentType.GetField(pathComponent,
						BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					while (field == null)
					{
						currentType = currentType.BaseType;
						field = currentType.GetField(pathComponent, BindingFlags.Instance | BindingFlags.NonPublic);
					}

					currentType = field.FieldType;
				}
			}

			return currentType;
		}

		public static string GetPropertyPath(this SerializedProperty property)
		{
			var propertyPath = property.propertyPath;
			var fullPath = "";
			var removeIndex = propertyPath.LastIndexOf('.');
			if (removeIndex >= 0)
				fullPath = propertyPath.Remove(removeIndex, propertyPath.Length - removeIndex) + '.';

			return fullPath;
		}

		public static SerializedProperty GetPropertyByPropertyLocalPath(this SerializedProperty property, string path)
		{
			var propertyPath = property.propertyPath;
			var fullPath = "";
			var removeIndex = propertyPath.LastIndexOf('.');
			if (removeIndex >= 0)
				fullPath = propertyPath.Remove(removeIndex, propertyPath.Length - removeIndex) + '.';
			fullPath += path;

			return property.serializedObject.FindProperty(fullPath);
		}

		public static Type[] GetCustomPropertyDrawerTypes(this CustomPropertyDrawer attribute)
		{
			var typeField = _customPropertyDrawerType.GetField(_customPropertyDrawerField_Type, _InternalFieldBindingFlags);
			var useForChildrenField = _customPropertyDrawerType.GetField(_customPropertyDrawerField_UseForChildren, _InternalFieldBindingFlags);

			var type = (Type)typeField.GetValue(attribute);
			var useForChildren = (bool)useForChildrenField.GetValue(attribute);

			return useForChildren ? GetInheritorTypesForSelection(type) : new[] { type };
		}

		public static void SetAttribute(this PropertyDrawer drawer, PropertyAttribute attribute)
		{
			var attributeField = _propertyDrawerType.GetField(_propertyDrawerField_Attribute, _InternalFieldBindingFlags);
			attributeField.SetValue(drawer, attribute);
		}

		public static void SetFieldInfo(this PropertyDrawer drawer, FieldInfo fieldInfo)
		{
			var fieldInfoField = _propertyDrawerType.GetField(_propertyDrawerField_FieldInfo, _InternalFieldBindingFlags);
			fieldInfoField.SetValue(drawer, fieldInfo);
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
