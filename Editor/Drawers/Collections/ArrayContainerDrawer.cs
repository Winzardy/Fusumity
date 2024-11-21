#if UNITY_EDITOR
using Fusumity.Collections;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Collections
{
	public class ArrayContainerDrawer : OdinValueDrawer<IArrayContainer>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			Property.Children[0].Draw(label);
		}
	}
}
#endif
