using System;
using System.Diagnostics;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Fusumity.Attributes
{
	[Conditional("UNITY_EDITOR")]
	public class ColorCardBoxAttribute : PropertyGroupAttribute
	{
		public float R { get; set; }
		public float G { get; set; }
		public float B { get; set; }
		public float A { get; set; }

		public string Label { get; }
		public bool UseLabelSeparator { get; set; }

		public ColorCardBoxAttribute(float r, float g, float b, float a = 1f, string groupId = "", float order = 0, string label = null,
			bool useLabelSeparator = false) : base(groupId, order)
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

		public DarkCardBoxAttribute(string groupId = "", float order = 0, string label = null, bool useLabelSeparator = false)
			: base(_color.r, _color.g, _color.b, _color.a, groupId, order, label, useLabelSeparator)
		{
		}
	}
}
