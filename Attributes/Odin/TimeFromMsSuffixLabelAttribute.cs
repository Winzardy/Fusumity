using System;
using System.Diagnostics;
using Sirenix.OdinInspector;

namespace Fusumity.Attributes
{
	[Conditional("UNITY_EDITOR")]
	[IncludeMyAttributes]
	[Unit(Units.Millisecond)]
	[SuffixLabel("@Fusumity.Editor.TimeSuffixLabelEditorHelper.MillisecondToTimespan($property)", true)]
	public class TimeFromMsSuffixLabelAttribute : Attribute
	{
	}

	[Conditional("UNITY_EDITOR")]
	[IncludeMyAttributes]
	[Unit(Units.Second)]
	[SuffixLabel("@Fusumity.Editor.TimeSuffixLabelEditorHelper.SecondToTimespan($property)", true)]
	public class TimeFromSecSuffixLabelAttribute : Attribute
	{
	}
}
