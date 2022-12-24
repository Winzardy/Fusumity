using System;

namespace Fusumity.Attributes.Specific
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class DisableIfAttribute : FusumityDrawerAttribute
	{
		public string checkPath;
		public object[] equalsAny;

		public DisableIfAttribute(string checkPath, params object[] equalsAny)
		{
			this.checkPath = checkPath;
			this.equalsAny = equalsAny;
		}
	}
}