using System;
using System.Diagnostics;
using UnityEngine;

namespace Fusumity.Attributes.Odin
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	[Conditional("UNITY_EDITOR")]
	public class HeaderTitleAttribute : Attribute
	{
		public string title;
		public int fontSize;
		public bool bold;
		public int lineThickness;
		public float topPadding;
		public Color? color;

		public HeaderTitleAttribute(string title, int fontSize = 11, bool bold = true, int lineThickness = 1, float topPadding = 4f)
		{
			this.title = title;
			this.fontSize = fontSize;
			this.bold = bold;
			this.lineThickness = lineThickness;
			this.topPadding = topPadding;
		}
	}
}
