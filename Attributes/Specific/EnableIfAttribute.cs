using System;

namespace Fusumity.Attributes.Specific
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class EnableIfAttribute : GenericDrawerAttribute
	{
		public string boolPath;

		public EnableIfAttribute(string boolPath)
		{
			this.boolPath = boolPath;
		}
	}
}
