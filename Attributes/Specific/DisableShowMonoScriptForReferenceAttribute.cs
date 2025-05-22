using System;
using System.Diagnostics;

namespace Fusumity.Attributes
{
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field)]
	public class DisableShowMonoScriptForReferenceAttribute : Attribute
	{
	}
}
