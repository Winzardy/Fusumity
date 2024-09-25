using Fusumity.Attributes.Specific;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	public class BackgroundColorDrawer<T> : OdinAttributeDrawer<T> where T : BackgroundColorAttribute
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var backgroundColorAttribute = (BackgroundColorAttribute)Attribute;

			var oldColor = GUI.backgroundColor;
			GUI.backgroundColor = backgroundColorAttribute.color;

			this.CallNextDrawer(label);

			GUI.backgroundColor = oldColor;
		}
	}
}
