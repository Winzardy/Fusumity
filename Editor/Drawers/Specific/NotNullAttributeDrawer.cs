using System;
using Content;
using Sapientia.Extensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.Utilities.Editor;
using UnityEngine;

[assembly: RegisterValidator(typeof(Fusumity.Editor.JetbrainsNotNullValidator))]
[assembly: RegisterValidator(typeof(Fusumity.Editor.NotNullValidator))]

namespace Fusumity.Editor
{
	using UnityObject = UnityEngine.Object;

	public class JetbrainsNotNullValidator : NotNullValidator<JetBrains.Annotations.NotNullAttribute>
	{
	}

	public class NotNullValidator : NotNullValidator<System.Diagnostics.CodeAnalysis.NotNullAttribute>
	{
	}

	public class NotNullValidator<T> : AttributeValidator<T>
		where T : System.Attribute
	{
		protected override void Validate(ValidationResult result)
		{
			var valueEntry = Property.ValueEntry;
			if (valueEntry == null)
				return;

			if (NotNullUtility.IsNull(valueEntry))
			{
				result.AddError($"Field '{Property.NiceName}' must not be null");
			}
		}
	}

	public class JetbrainsNotNullAttributeDrawer : OdinAttributeDrawer<JetBrains.Annotations.NotNullAttribute>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var valueEntry = Property.ValueEntry;

			if (valueEntry == null)
				return;

			var isNull = NotNullUtility.IsNull(valueEntry);

			var originColor = GUI.color;

			if (isNull)
				GUI.color = NotNullUtility.GetColor(originColor);

			CallNextDrawer(label);
			GUI.color = originColor;
		}
	}

	public class NotNullAttributeDrawer : OdinAttributeDrawer<System.Diagnostics.CodeAnalysis.NotNullAttribute>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var valueEntry = Property.ValueEntry;

			if (valueEntry == null)
				return;

			var isNull = NotNullUtility.IsNull(valueEntry);

			var originColor = GUI.color;
			if (isNull)
				GUI.color = NotNullUtility.GetColor(originColor);

			CallNextDrawer(label);
			GUI.color = originColor;
		}
	}

	public static class NotNullUtility
	{
		public static Color GetColor(Color original)
		{
			return Color.Lerp(original, SirenixGUIStyles.RedErrorColor, 0.8f);
		}

		public static bool IsNull(IPropertyValueEntry valueEntry)
		{
			if (valueEntry.TypeOfValue.IsSubclassOf(typeof(UnityObject)))
			{
				var unityObject = valueEntry.WeakSmartValue as UnityObject;
				return unityObject == null;
			}

			return valueEntry.WeakSmartValue == null;
		}
	}
}
