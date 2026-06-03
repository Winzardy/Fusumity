using Fusumity.Attributes.Odin;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	[DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
	public class HeaderTitleAttributeDrawer : OdinAttributeDrawer<HeaderTitleAttribute>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (Attribute.topPadding > 0)
				GUILayout.Space(Attribute.topPadding);

			var style = new GUIStyle(GUI.skin.label)
			{
				fontSize = Attribute.fontSize,
				fontStyle = Attribute.bold ? FontStyle.Bold : FontStyle.Normal
			};

			Rect rect = EditorGUILayout.GetControlRect(false, style.CalcHeight(new GUIContent(Attribute.title), EditorGUIUtility.currentViewWidth));
			EditorGUI.LabelField(rect, Attribute.title, style);

			var color = Attribute.color ?? SirenixGUIStyles.BorderColor;
			SirenixEditorGUI.HorizontalLineSeparator(color, Attribute.lineThickness);

			CallNextDrawer(label);
		}
	}
}
