using System;

namespace Fusumity.Attributes.Specific
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class FastAssetSelectorAttribute : Attribute
	{
		public Type type;

		public FastAssetSelectorAttribute(Type type = null)
		{
			this.type = type;
		}
	}
}
