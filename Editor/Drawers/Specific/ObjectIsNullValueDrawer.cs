using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	using UnityObject = UnityEngine.Object;

	public class UnityObjectAttributeProcessor : OdinAttributeProcessor<UnityObject>
	{
		public override void ProcessSelfAttributes(
			InspectorProperty property,
			List<Attribute> attributes)
		{
			if (attributes.Exists(x => x is ObjectNullAttribute))
				return;
			attributes.Add(new ObjectNullAttribute());
		}
	}

	public class ObjectNullAttribute : Attribute
	{
	}

	public class ObjectNullAttributeDrawer : OdinAttributeDrawer<ObjectNullAttribute>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var valueEntry = Property.ValueEntry;
			var isValid = ObjectIsNullUtility.Validate(valueEntry);
			var originColor = GUI.color;
			if (!isValid)
				GUI.color = ObjectIsNullUtility.GetWarningColor(originColor);
			CallNextDrawer(label);
			GUI.color = originColor;
		}
	}

	public static class ObjectIsNullUtility
	{
		public static Color GetInvalidColor(Color original)
		{
			return Color.Lerp(original, SirenixGUIStyles.RedErrorColor, 0.8f);
		}

		public static Color GetWarningColor(Color original)
		{
			return Color.Lerp(original, SirenixGUIStyles.YellowWarningColor, 0.8f);
		}

		public static bool Validate(IPropertyValueEntry valueEntry)
		{
			if (!GUI.enabled)
				return true;

			var property = valueEntry.Property;
			if (property != null)
			{
				if (!property.State.Visible)
					return true;

				if (!property.State.Enabled)
					return true;

				if (property.Info.GetMemberInfo() is not FieldInfo fieldInfo)
					return true;

				if (HasNullableAttribute(property))
					return true;

				// ignore Unity internal structs/classes
				if (IsUnityOwnedType(fieldInfo.DeclaringType))
					return true;
			}

			if (typeof(UnityObject).IsAssignableFrom(valueEntry.TypeOfValue))
			{
				var unityObject = valueEntry.WeakSmartValue as UnityObject;
				return unityObject != null;
			}

			return valueEntry.WeakSmartValue != null;
		}

		private static bool HasNullableAttribute(InspectorProperty property)
		{
			if (property.Info.GetAttribute<CanBeNullAttribute>() != null)
				return true;
			if (property.Info.GetAttribute<MaybeNullAttribute>() != null)
				return true;
			if (property.Info.GetAttribute<OptionalSuffixLabel>() != null)
				return true;

			return false;
		}

		private static bool IsUnityOwnedType(Type type)
		{
			if (type.Assembly == typeof(UnityEngine.Object).Assembly)
				return true;
			return type.Namespace != null &&
				(type.Namespace.StartsWith("UnityEngine") ||
					type.Namespace.StartsWith("UnityEditor"));
		}
	}
}
