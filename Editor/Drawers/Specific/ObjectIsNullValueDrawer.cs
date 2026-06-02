using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;
using Sapientia;
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
			var validationState = ObjectIsNullUtility.ValidateState(valueEntry);
			var originColor = GUI.color;
			switch (validationState)
			{
				case ObjectIsNullUtility.ValidationState.Warning:
					GUI.color = ObjectIsNullUtility.GetWarningColor(originColor);
					break;
				case ObjectIsNullUtility.ValidationState.Invalid:
					GUI.color = ObjectIsNullUtility.GetInvalidColor(originColor);
					break;
			}

			CallNextDrawer(label);
			GUI.color = originColor;
		}
	}

	public static class ObjectIsNullUtility
	{
		public enum ValidationState
		{
			Valid = 0,
			Warning = 1,
			Invalid = 2,
		}

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
			return ValidateState(valueEntry) == ValidationState.Valid;
		}

		public static ValidationState ValidateState(IPropertyValueEntry valueEntry)
		{
			if (!GUI.enabled)
				return ValidationState.Valid;

			var property = valueEntry.Property;
			if (property != null)
			{
				if (!property.State.Visible)
					return ValidationState.Valid;

				if (!property.State.Enabled)
					return ValidationState.Valid;

				if (property.Info.GetMemberInfo() is not FieldInfo fieldInfo)
					return ValidationState.Valid;

				// ignore Unity internal structs/classes
				if (IsUnityOwnedType(fieldInfo.DeclaringType))
					return ValidationState.Valid;
			}

			if (!IsNull(valueEntry))
				return ValidationState.Valid;

			if (property != null && HasRequiredAttribute(property))
				return ValidationState.Invalid;

			if (property != null && HasNullableAttribute(property))
				return ValidationState.Valid;

			return ValidationState.Warning;
		}

		private static bool IsNull(IPropertyValueEntry valueEntry)
		{
			if (typeof(UnityObject).IsAssignableFrom(valueEntry.TypeOfValue))
			{
				var unityObject = valueEntry.WeakSmartValue as UnityObject;
				return unityObject == null;
			}

			return valueEntry.WeakSmartValue == null;
		}

		public static bool HasRequiredAttribute(InspectorProperty property)
		{
			if (property == null)
				return false;

			if (property.Info.GetAttribute<NotEmptyAttribute>() != null)
				return true;
			if (property.Info.GetAttribute<JetBrains.Annotations.NotNullAttribute>() != null)
				return true;
			if (property.Info.GetAttribute<System.Diagnostics.CodeAnalysis.NotNullAttribute>() != null)
				return true;

			return false;
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
