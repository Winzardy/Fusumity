using System;
using System.Diagnostics;
using UnityEngine;

namespace Fusumity.Attributes.Specific
{
	[Conditional("UNITY_EDITOR")]
	public class SuffixValueAttribute : Attribute
	{
		public string Text { get; private set; }
		public GUIContent Label { get; private set; }

		public SuffixValueAttribute(string text, GUIContent label = null)
		{
			Text = text;
			Label = label;
		}
	}
}
