using System;

namespace Fusumity.Attributes.Specific
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class HideIfAttribute : GenericDrawerAttribute
	{
		public string boolPath;

		public HideIfAttribute(string boolPath)
		{
			this.boolPath = boolPath;
		}
	}
}
