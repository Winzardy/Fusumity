using System;
using System.Diagnostics;

namespace Fusumity.Attributes
{
	[Conditional("UNITY_EDITOR")]
	public class TimeFromMsSuffixLabelParentAttribute : Attribute, IAttributeConvertible
	{
		public Attribute Convert()
			=> new TimeFromMsSuffixLabelAttribute();
	}

	[Conditional("UNITY_EDITOR")]
	public class TimeFromSecSuffixLabelParentAttribute : Attribute, IAttributeConvertible
	{
		public Attribute Convert()
			=> new TimeFromSecSuffixLabelAttribute();
	}
}
