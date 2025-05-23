using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sirenix.OdinInspector;

namespace Distribution.Editor
{
	[Conditional("UNITY_EDITOR")]
	[IncludeMyAttributes]
	[ValueDropdown("@" + nameof(StorePlatformDropdownAttributeHelper) + "." + nameof(StorePlatformDropdownAttributeHelper.GetAllPlatforms) +
		"()")]
	public class StorePlatformDropdownAttribute : Attribute
	{
	}

	public static class StorePlatformDropdownAttributeHelper
	{
		public static IEnumerable<ValueDropdownItem<string>> GetAllPlatforms() =>
			StorePlatformExt.GetAllPlatforms().Select(entry => new ValueDropdownItem<string>(entry.ToLabel(), entry));
	}
}
