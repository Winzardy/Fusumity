using System;
using System.Diagnostics;
using Fusumity.Attributes;

namespace Localizations
{
	[Conditional("UNITY_EDITOR")]
	public class LocKeyParentAttribute : ParentAttribute
	{
		public string FieldName { get; private set; }

		public LocKeyParentAttribute(string fieldName = LocKeyAttribute.DEFAULT_FIELD_NAME)
		{
			FieldName = fieldName;
		}

		public override Attribute Convert()
			=> new LocKeyAttribute(FieldName);
	}
}
