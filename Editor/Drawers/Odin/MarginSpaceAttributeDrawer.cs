using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	public class MarginSpaceAttributeDrawer : OdinAttributeDrawer<MarginSpaceAttribute>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			GUILayout.BeginVertical();

			if (Attribute.top != 0)
				GUILayout.Space(Attribute.top);

			GUILayout.BeginHorizontal();

			if (Attribute.left != 0)
				GUILayout.Space(Attribute.left);

			this.CallNextDrawer(label);

			if (Attribute.right != 0)
				GUILayout.Space(Attribute.right);

			GUILayout.EndHorizontal();

			if (Attribute.top != 0)
				GUILayout.Space(Attribute.bottom);

			GUILayout.EndVertical();
		}
	}
}
