using System;

namespace Fusumity.Attributes.Specific
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class EnableIfAttribute : FusumityDrawerAttribute
	{
		public string checkPath;
		public object[] equalsAny;

		public EnableIfAttribute(string checkPath, params object[] equalsAny)
		{
			this.checkPath = checkPath;
			this.equalsAny = equalsAny;
		}
	}
}