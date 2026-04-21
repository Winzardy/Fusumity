using System;
using System.Diagnostics;
using UnityEngine;

namespace Fusumity.Attributes
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
	[Conditional("UNITY_EDITOR")]
	public class FoldoutContainerAttribute : PropertyAttribute
	{
		private static Color DEFAULT_BOX_COLOR = new(0.9f, 0.9f, 0.9f, 0.1f);
		public bool ExpandedByDefault { get; }
		public bool UseBox { get; }
		public Color BoxColor { get; }

		public FoldoutContainerAttribute(bool expandedByDefault = false, bool useBox = false)
		{
			ExpandedByDefault = expandedByDefault;
			UseBox            = useBox;
			BoxColor          = DEFAULT_BOX_COLOR;
		}

		public FoldoutContainerAttribute(bool expandedByDefault, bool useBox, Color colorBox)
		{
			ExpandedByDefault = expandedByDefault;
			UseBox            = useBox;
			BoxColor          = colorBox;
		}
	}
}
