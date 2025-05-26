using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace UI.Editor
{
	public class UIArgsDarkBoxAttribute : Attribute
	{
	}

	public class UITabDarkBoxDrawer<T> : OdinAttributeDrawer<UIArgsDarkBoxAttribute, T>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			GUIHelper.PushColor(Color.black * .5f);
			{
				SirenixEditorGUI.BeginBox();
			}
			GUIHelper.PopColor();

			CallNextDrawer(label);
			SirenixEditorGUI.EndBox();
		}
	}
}
