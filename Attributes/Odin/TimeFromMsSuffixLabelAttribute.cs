using System;
using System.Diagnostics;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector;

namespace Fusumity.Attributes
{
	[Conditional("UNITY_EDITOR")]
	[IncludeMyAttributes]
	[Minimum(0)]
	[SuffixLabel("@Fusumity.Editor.TimeSuffixLabelEditorHelper.MillisecondToTimespan($property)", true)]
	public class TimeFromMsSuffixLabelAttribute : Attribute
	{
	}
}
