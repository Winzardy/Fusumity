using System;
using System.Diagnostics;

namespace Fusumity.Attributes.Odin
{
	[Conditional("UNITY_EDITOR")]
	public class ConstDropdownAttribute : Attribute
	{
		public Type Type { get; }
		public string TypeName { get; }
		public bool PrettyPrint { get; } = true;

		/// <summary>
		/// Make sure that there is default (None) value to select.
		/// </summary>
		public bool EnsureDefaultValue { get; set; } = true;

		/// <summary>
		/// Displays underlying value in format: "VariableName (variableValue)"
		/// </summary>
		public bool DisplayValue { get; set; } = false;

		public ConstDropdownAttribute(Type type)
		{
			this.Type = type;
		}

		public ConstDropdownAttribute(Type type, bool prettyPrint)
		{
			this.Type = type;
			this.PrettyPrint = prettyPrint;
		}

		public ConstDropdownAttribute(string typeName)
		{
			this.TypeName = typeName;
		}

		public ConstDropdownAttribute(string typeName, bool prettyPrint)
		{
			this.TypeName = typeName;
			this.PrettyPrint = prettyPrint;
		}
	}
}
