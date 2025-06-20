using System;

namespace Localizations
{
	[AttributeUsage(AttributeTargets.Field)]
	public class LocKeyAttribute : Attribute
	{
		internal const string DEFAULT_FIELD_NAME = "_languageEditor";
		public string FieldName { get; private set; }

		public LocKeyAttribute(string fieldName = DEFAULT_FIELD_NAME) => FieldName = fieldName;
	}
}
