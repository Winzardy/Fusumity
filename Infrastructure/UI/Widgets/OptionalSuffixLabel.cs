using System;
using System.Diagnostics;
using Sirenix.OdinInspector;

namespace UI
{
	[IncludeMyAttributes]
	[SuffixLabel("Optional          ", true)]
	[Conditional("UNITY_EDITOR")]
	public class OptionalSuffixLabel : Attribute
	{
	}
}
