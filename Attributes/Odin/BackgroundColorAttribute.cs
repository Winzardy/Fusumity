using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Fusumity.Attributes.Specific
{
	[DontApplyToListElements]
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class BackgroundColorAttribute : Attribute
	{
		public Color color;

		public BackgroundColorAttribute(float r, float g, float b, float a = 1f)
		{
			color = new Color(r, g, b, a);
		}

		public BackgroundColorAttribute(BackgroundColor color, float a = 1f)
		{
			this.color = color switch
			{
				BackgroundColor.Red => Color.red,
				BackgroundColor.Green => Color.green,
				BackgroundColor.Blue => Color.blue,
				BackgroundColor.White => Color.white,
				BackgroundColor.Black => Color.black,
				BackgroundColor.Gray => Color.gray,
				BackgroundColor.Magenta => Color.magenta,
				BackgroundColor.Yellow => Color.yellow,
				BackgroundColor.Cyan => Color.cyan,
				BackgroundColor.Clear => Color.clear,
			};
			if (color != BackgroundColor.Clear)
				this.color.a = a;
		}
	}

	public enum BackgroundColor
	{
		Red,
		Green,
		Blue,
		White,
		Black,
		Gray,
		Magenta,
		Yellow,
		Cyan,
		Clear,
	}
}
