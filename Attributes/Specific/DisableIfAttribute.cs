using System;

namespace Fusumity.Attributes.Specific
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class DisableIfAttribute : GenericDrawerAttribute
	{
		public string boolPath;

		public DisableIfAttribute(string boolPath)
		{
			this.boolPath = boolPath;
		}
	}
}
