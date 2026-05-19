using Fusumity.Editor;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Content.Editor
{
	using UnityObject = Object;

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
