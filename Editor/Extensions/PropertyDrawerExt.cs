using System;
using System.Reflection;
using Fusumity.Editor.Drawers;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Extensions
{
	public static class PropertyDrawerExt
	{
		private static readonly Type _customPropertyDrawerType = typeof(CustomPropertyDrawer);
		private const string _customPropertyDrawerField_Type = "m_Type";
		private const string _customPropertyDrawerField_UseForChildren = "m_UseForChildren";

		private static readonly Type _propertyDrawerType = typeof(PropertyDrawer);
		private const string _propertyDrawerField_Attribute = "m_Attribute";
		private const string _propertyDrawerField_FieldInfo = "m_FieldInfo";

		private const string _customPropertyDrawerMethod_DrawLabel = "DrawLabel";
		private const string _customPropertyDrawerMethod_DrawSubBody = "DrawSubBody";
		private const string _customPropertyDrawerMethod_DrawBody = "DrawBody";

		public static Type[] GetCustomPropertyDrawerTypes(this CustomPropertyDrawer drawer)
		{
			var typeField = _customPropertyDrawerType.GetField(_customPropertyDrawerField_Type, ReflectionExt.internalFieldBindingFlags);
			var useForChildrenField = _customPropertyDrawerType.GetField(_customPropertyDrawerField_UseForChildren, ReflectionExt.internalFieldBindingFlags);

			var type = (Type)typeField.GetValue(drawer);
			var useForChildren = (bool)useForChildrenField.GetValue(drawer);

			return useForChildren ? type.GetInheritorTypes() : new[] { type };
		}

		public static void SetAttribute(this PropertyDrawer drawer, PropertyAttribute attribute)
		{
			var attributeField = _propertyDrawerType.GetField(_propertyDrawerField_Attribute, ReflectionExt.internalFieldBindingFlags);
			attributeField.SetValue(drawer, attribute);
		}

		public static void SetFieldInfo(this PropertyDrawer drawer, FieldInfo fieldInfo)
		{
			var fieldInfoField = _propertyDrawerType.GetField(_propertyDrawerField_FieldInfo, ReflectionExt.internalFieldBindingFlags);
			fieldInfoField.SetValue(drawer, fieldInfo);
		}

		public static bool IsDrawLabelOverriden(this FusumityPropertyDrawer drawer)
		{
			return drawer.IsDrawerMethodOverriden(_customPropertyDrawerMethod_DrawLabel);
		}

		public static bool IsDrawSubBodyOverriden(this FusumityPropertyDrawer drawer)
		{
			return drawer.IsDrawerMethodOverriden(_customPropertyDrawerMethod_DrawSubBody);
		}

		public static bool IsDrawBodyOverriden(this FusumityPropertyDrawer drawer)
		{
			return drawer.IsDrawerMethodOverriden(_customPropertyDrawerMethod_DrawBody);
		}

		private static bool IsDrawerMethodOverriden(this FusumityPropertyDrawer drawer, string name)
		{
			var drawerType = drawer.GetType();
			var methodInfo = drawerType.GetMethod(name, ReflectionExt.overridenMethodBindingFlags, null, new [] {typeof(Rect)}, null);

			return methodInfo != null;
		}
	}
}