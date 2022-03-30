using System;

namespace Fusumity.Attributes.Specific
{
	public class ReferenceSelectionAttribute : GenericDrawerAttribute
	{
		public Type type;
		public bool insertNull = true;

		public ReferenceSelectionAttribute(Type type = null)
		{
			this.type = type;
		}
	}
}
