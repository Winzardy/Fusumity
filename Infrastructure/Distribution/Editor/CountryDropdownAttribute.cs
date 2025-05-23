using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sirenix.OdinInspector;

namespace Distribution.Editor
{
	[Conditional("UNITY_EDITOR")]
	[IncludeMyAttributes]
	[ValueDropdown("@" + nameof(CountryDropdownAttributeHelper) + "." + nameof(CountryDropdownAttributeHelper.GetAll) +
		"()")]
	public class CountryDropdownAttribute : Attribute
	{
	}

	public static class CountryDropdownAttributeHelper
	{
		public static IEnumerable<ValueDropdownItem<string>> GetAll() =>
			CountryEntryExt.GetAll().Select(@ref => new ValueDropdownItem<string>(@ref.entry.ToLabel(), @ref.entry));
	}
}
