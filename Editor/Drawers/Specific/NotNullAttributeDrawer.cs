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

			if (valueEntry.TypeOfValue.IsSubclassOf(typeof(UnityObject)))
			{
				var unityObject = valueEntry.WeakSmartValue as UnityObject;
				if (unityObject == null)
				{
					result.ResultType = ValidationResultType.Error;
					result.Message = $"Field '{Property.NiceName}' must not be null.";
				}
			}
			else
			{
				if (valueEntry.WeakSmartValue == null)
				{
					result.ResultType = ValidationResultType.Error;
					result.Message = $"Field '{Property.NiceName}' must not be null.";
				}
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

			bool hasError;
			var value = valueEntry.WeakSmartValue;
			var isNull = value is Object obj ? obj == null : value == null;
			if (value is string str)
				hasError = str.IsNullOrWhiteSpace();
			else
				hasError = isNull;

			var originColor = GUI.color;
			if (hasError)
				GUI.color = Color.Lerp(originColor, SirenixGUIStyles.RedErrorColor, 0.8f);
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

			bool hasError;
			var value = valueEntry.WeakSmartValue;
			var isNull = value is Object obj ? obj == null : value == null;
			if (value is string str)
				hasError = str.IsNullOrWhiteSpace();
			else
				hasError = isNull;
			var originColor = GUI.color;
			if (hasError)
				GUI.color = Color.Lerp(originColor, SirenixGUIStyles.RedErrorColor, 0.8f);
			CallNextDrawer(label);
			GUI.color = originColor;
		}
	}
}
