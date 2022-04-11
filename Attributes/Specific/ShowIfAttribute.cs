using System;

namespace Fusumity.Attributes.Specific
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class ShowIfAttribute : FusumityDrawerAttribute
	{
		public string boolPath;

		public ShowIfAttribute(string boolPath)
		{
			this.boolPath = boolPath;
		}
	}
}
