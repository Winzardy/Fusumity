using System;
using UnityEngine;

namespace Fusumity.Attributes
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class FusumityDrawerAttribute : PropertyAttribute {}

	public interface IFusumitySerializable {}
}