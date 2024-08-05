using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	public class UrlButtonDrawer<T> : OdinAttributeDrawer<T> where T : UrlButtonAttribute
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (Attribute.width >= 0)
				EditorGUILayout.BeginHorizontal(GUILayout.Width(Attribute.width));

			if (SirenixEditorGUI.SDFIconButton(Attribute.label, EditorGUIUtility.singleLineHeight, Attribute.icon))
				Application.OpenURL(Attribute.url);

			if (Attribute.width >= 0)
				EditorGUILayout.EndHorizontal();

			CallNextDrawer(label);
		}
	}
}
