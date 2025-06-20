using Fusumity.Attributes;
using Fusumity.Utility;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	public class DarkCardBoxAttributeDrawer<T> : OdinAttributeDrawer<DarkCardBoxAttribute, T>
	{
		private static readonly GUIStyle _style = new(SirenixGUIStyles.CardStyle)
		{
			padding = new RectOffset(5, 3, 2, 3),
			margin = new RectOffset
			(
				SirenixGUIStyles.CardStyle.margin.left + 3,
				SirenixGUIStyles.CardStyle.margin.right + 3,
				SirenixGUIStyles.CardStyle.margin.top + 2,
				SirenixGUIStyles.CardStyle.margin.bottom
			)
		};

		protected override void DrawPropertyLayout(GUIContent label)
		{
			GUIHelper.PushColor(Color.black.WithAlpha(0.666f));
			SirenixEditorGUI.BeginIndentedVertical(_style);
			{
				GUIHelper.PushHierarchyMode(false);

				GUIHelper.PopColor();

				CallNextDrawer(label);
				GUIHelper.PopHierarchyMode();
			}
			SirenixEditorGUI.EndIndentedVertical();
		}
	}
}
