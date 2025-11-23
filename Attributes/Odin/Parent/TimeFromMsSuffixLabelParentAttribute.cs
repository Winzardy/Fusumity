using System;
using System.Diagnostics;

namespace Fusumity.Attributes
{
	[Conditional("UNITY_EDITOR")]
	public class TimeFromMsSuffixLabelParentAttribute : ParentAttribute
	{
		public override Attribute Convert()
			=> new TimeFromMsSuffixLabelAttribute();
	}

	[Conditional("UNITY_EDITOR")]
	public class TimeFromSecSuffixLabelParentAttribute : ParentAttribute
	{
		public override Attribute Convert()
			=> new TimeFromSecSuffixLabelAttribute();
	}
}
