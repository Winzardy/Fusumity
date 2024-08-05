using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	public class UrlHeaderDrawer : OdinAttributeDrawer<UrlHeaderAttribute>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var color = GUI.color;
			GUI.color = Attribute.color;

			EditorGUILayout.LabelField($"URL: {Attribute.label}");
			if (Event.current.type == EventType.MouseUp && Property.LastDrawnValueRect.Contains(Event.current.mousePosition))
				Application.OpenURL(Attribute.url);

			GUI.color = color;

			CallNextDrawer(label);
		}
	}
}
