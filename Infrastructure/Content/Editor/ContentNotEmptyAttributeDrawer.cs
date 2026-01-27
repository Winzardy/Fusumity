using System;
using Content.Editor;
using Fusumity.Editor;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.Utilities.Editor;
using UnityEngine;

[assembly: RegisterValidator(typeof(ContentNotEmptyValidator))]

namespace Content.Editor
{
	using UnityObject = UnityEngine.Object;

	public class ContentNotEmptyValidator : ContentNotEmptyValidator<NotEmptyAttribute>
	{
	}

	public static class ContentNotEmptyUtility
	{
		internal static bool IsNull(IContentEntry contentEntry)
		{
			if (contentEntry.RawValue == null)
				return true;

			if (contentEntry.ValueType.IsSubclassOf(typeof(UnityObject)))
			{
				var unityObject = contentEntry.RawValue as UnityObject;
				return unityObject == null;
			}

			return contentEntry.RawValue == null;
		}
	}

	public class ContentNotEmptyValidator<T> : AttributeValidator<T>
		where T : Attribute
	{
		protected override void Validate(ValidationResult result)
		{
			var valueEntry = Property.ValueEntry;

			if (valueEntry == null)
				return;

			if (valueEntry.WeakSmartValue is IContentReference reference)
				if (reference.IsEmpty())
					result.AddError($"Content Reference '{Property.NiceName}' must not be empty");

			if (valueEntry.WeakSmartValue is IContentEntry entry)
				if (ContentNotEmptyUtility.IsNull(entry))
					result.AddError($"Content Entry '{Property.NiceName}' must not be null");
		}
	}

	public class ContentNotEmptyAttributeDrawer : OdinAttributeDrawer<NotEmptyAttribute>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var valueEntry = Property.ValueEntry;
			if (valueEntry == null)
				return;

			var hasError = false;
			if (valueEntry.WeakSmartValue is IContentReference reference)
			{
				hasError = reference.IsEmpty();
			}
			else if (valueEntry.WeakSmartValue is IContentEntry entry)
			{
				hasError = ContentNotEmptyUtility.IsNull(entry);
			}

			var originColor = GUI.color;
			if (hasError)
				GUI.color = NotNullUtility.GetColor(originColor);
			CallNextDrawer(label);
			GUI.color = originColor;
		}
	}

	public class ContentJetbrainsNotNullAttributeDrawer : OdinAttributeDrawer<NotNullAttribute>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var valueEntry = Property.ValueEntry;
			if (valueEntry == null)
				return;

			var hasError = false;
			if (valueEntry.WeakSmartValue is IContentReference reference)
			{
				hasError = reference.IsEmpty();
			}
			else if (valueEntry.WeakSmartValue is IContentEntry entry)
			{
				hasError = ContentNotEmptyUtility.IsNull(entry);
			}

			var originColor = GUI.color;
			if (hasError)
				GUI.color = NotNullUtility.GetColor(originColor);
			CallNextDrawer(label);
			GUI.color = originColor;
		}
	}

	public class ContentNotNullAttributeDrawer : OdinAttributeDrawer<System.Diagnostics.CodeAnalysis.NotNullAttribute>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var valueEntry = Property.ValueEntry;
			if (valueEntry == null)
				return;

			var hasError = false;
			if (valueEntry.WeakSmartValue is IContentReference reference)
			{
				hasError = reference.IsEmpty();
			}
			else if (valueEntry.WeakSmartValue is IContentEntry entry)
			{
				hasError = ContentNotEmptyUtility.IsNull(entry);
			}

			var originColor = GUI.color;
			if (hasError)
				GUI.color = NotNullUtility.GetColor(originColor);
			CallNextDrawer(label);
			GUI.color = originColor;
		}
	}
}
