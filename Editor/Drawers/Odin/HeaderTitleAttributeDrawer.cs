using Fusumity.Attributes.Odin;
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
			//if (Attribute.topPadding > 0)
			//	GUILayout.Space(Attribute.topPadding);

			//// Используем тот же метод что и TitleAttributeDrawer от Odin
			//SirenixEditorGUI.Title(
			//	Attribute.title,
			//	"",                          // subtitle
			//	TextAlignment.Left,
			//	Attribute.lineThickness > 0, // horizontalLine
			//	Attribute.bold
			//);

			//CallNextDrawer(label);

			//if (Attribute.topPadding > 0)
			//	GUILayout.Space(Attribute.topPadding);

			//var style = new GUIStyle(SirenixGUIStyles.BoldTitle)
			//{
			//	fontSize = Attribute.fontSize,
			//	fontStyle = Attribute.bold ? FontStyle.Bold : FontStyle.Normal,
			//	richText = true,
			//};

			//GUILayout.Label(Attribute.title, style);

			//var color = Attribute.color ?? SirenixGUIStyles.BorderColor;
			//SirenixEditorGUI.HorizontalLineSeparator(color, Attribute.lineThickness);

			//GUILayout.Space(2);

			//CallNextDrawer(label);

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
