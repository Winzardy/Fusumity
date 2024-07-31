using System;
using System.Diagnostics;

namespace Fusumity.Attributes.Odin
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
	[Conditional("UNITY_EDITOR")]
	public class AngleToRadAttribute : Attribute {}
}