using System;
using System.Diagnostics;

namespace Sapientia
{
	[Conditional("UNITY_EDITOR")]
	public class ToggleOffsetAttribute : Attribute
	{
		public int Offset { get; }

		public ToggleOffsetAttribute(int offset) => Offset = offset;
	}
}
