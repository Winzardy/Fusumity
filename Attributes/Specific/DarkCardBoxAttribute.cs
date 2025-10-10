using System;
using System.Diagnostics;
using Fusumity.Utility;
using UnityEngine;

namespace Fusumity.Attributes
{
	[Conditional("UNITY_EDITOR")]
	public class ColorCardBoxAttribute : Attribute
	{
		public float R { get; set; }
		public float G { get; set; }
		public float B { get; set; }
		public float A { get; set; }

		public string Label { get; }
		public bool UseLabelSeparator { get; set; }

		public ColorCardBoxAttribute(float r, float g, float b, float a = 1f, string label = null, bool useLabelSeparator = false)
		{
			R = r;
			G = g;
			B = b;
			A = a;

			Label = label;
			UseLabelSeparator = useLabelSeparator;
		}
	}

	[Conditional("UNITY_EDITOR")]
	public class DarkCardBoxAttribute : ColorCardBoxAttribute
	{
		private static readonly Color _color = Color.black
		   .WithAlpha(0.666f);

		public DarkCardBoxAttribute(string label = null, bool useLabelSeparator = false)
			: base(_color.r, _color.g, _color.b, _color.a, label, useLabelSeparator)
		{
		}
	}
}
