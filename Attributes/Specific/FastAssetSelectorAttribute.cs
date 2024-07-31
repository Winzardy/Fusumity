using System;

namespace Fusumity.Attributes.Specific
{
	public class FastAssetSelectorAttribute : FusumityDrawerAttribute
	{
		public Type type;

		public FastAssetSelectorAttribute(Type type = null)
		{
			this.type = type;
		}
	}
}