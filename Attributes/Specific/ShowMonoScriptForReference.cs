using System;
using System.Diagnostics;

namespace Fusumity.Attributes
{
	[Conditional("UNITY_EDITOR")]
	public class ShowMonoScriptForReferenceAttribute : Attribute
	{
	}
}
