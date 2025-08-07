using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	public class SingleFieldInlineDrawer : OdinAttributeDrawer<SingleFieldInlineAttribute>
	{
		protected override bool CanDrawAttributeProperty(InspectorProperty property)
		{
			return base.CanDrawAttributeProperty(property) && property.Children.Count == 1;
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			Property.Children[0].Draw(label);
		}
	}
}
