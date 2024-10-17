#if UNITY_EDITOR
using Fusumity.Collections;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Collections
{
	public class EnumArrayDrawer : OdinValueDrawer<IEnumArray>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			Property.Children[0].Draw(label);
		}
	}
}
#endif
