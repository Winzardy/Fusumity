using Sapientia.Extensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Generic
{
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
