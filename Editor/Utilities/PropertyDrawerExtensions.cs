using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Utilities
{
	public static class PropertyDrawerExtensions
	{
		private const BindingFlags _internalFieldBindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;

		private static readonly Type _customPropertyDrawerType = typeof(CustomPropertyDrawer);
		private const string _customPropertyDrawerField_Type = "m_Type";
		private const string _customPropertyDrawerField_UseForChildren = "m_UseForChildren";

		private static readonly Type _propertyDrawerType = typeof(PropertyDrawer);
		private const string _propertyDrawerField_Attribute = "m_Attribute";
		private const string _propertyDrawerField_FieldInfo = "m_FieldInfo";

		public static Type[] GetCustomPropertyDrawerTypes(this CustomPropertyDrawer attribute)
		{
			var typeField = _customPropertyDrawerType.GetField(_customPropertyDrawerField_Type, _internalFieldBindingFlags);
			var useForChildrenField = _customPropertyDrawerType.GetField(_customPropertyDrawerField_UseForChildren, _internalFieldBindingFlags);

			var type = (Type)typeField.GetValue(attribute);
			var useForChildren = (bool)useForChildrenField.GetValue(attribute);

			return useForChildren ? type.GetInheritorTypes() : new[] { type };
		}

		public static void SetAttribute(this PropertyDrawer drawer, PropertyAttribute attribute)
		{
			var attributeField = _propertyDrawerType.GetField(_propertyDrawerField_Attribute, _internalFieldBindingFlags);
			attributeField.SetValue(drawer, attribute);
		}

		public static void SetFieldInfo(this PropertyDrawer drawer, FieldInfo fieldInfo)
		{
			var fieldInfoField = _propertyDrawerType.GetField(_propertyDrawerField_FieldInfo, _internalFieldBindingFlags);
			fieldInfoField.SetValue(drawer, fieldInfo);
		}
	}
}
