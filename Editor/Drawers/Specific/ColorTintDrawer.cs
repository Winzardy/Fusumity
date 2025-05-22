using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	/// <summary>
	/// Интересный вариант показать цветом вложеность...
	/// </summary>
	[DrawerPriority(0, 1, 0)]
	public abstract class ColorTintDrawer<T> : OdinValueDrawer<T>
	{
		protected virtual float Factor => 0.1f;
		protected abstract Color Color { get; }

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var origin = GUI.color;
			GUI.color = Color.Lerp(origin, Color, Factor);
			CallNextDrawer(label);
			GUI.color = origin;
		}
	}
}
