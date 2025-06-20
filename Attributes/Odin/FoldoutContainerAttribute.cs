using System;
using System.Diagnostics;
using UnityEngine;

namespace Fusumity.Attributes
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
	[Conditional("UNITY_EDITOR")]
	public class FoldoutContainerAttribute : PropertyAttribute
	{
	}
}
