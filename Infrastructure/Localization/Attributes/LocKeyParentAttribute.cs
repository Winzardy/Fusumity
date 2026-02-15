using System;
using System.Diagnostics;
using Fusumity.Attributes;

namespace Localization
{
	[Conditional("UNITY_EDITOR")]
	public class LocKeyParentAttribute : Attribute, IAttributeConvertible
	{
		public string FieldName { get; private set; }

		public LocKeyParentAttribute(string fieldName = LocKeyAttribute.DEFAULT_FIELD_NAME)
		{
			FieldName = fieldName;
		}

		public Attribute Convert()
			=> new LocKeyAttribute(FieldName);
	}
}
