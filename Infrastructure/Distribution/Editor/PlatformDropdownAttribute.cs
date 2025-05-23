using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sirenix.OdinInspector;

namespace Distribution.Editor
{
	[Conditional("UNITY_EDITOR")]
	[IncludeMyAttributes]
	[ValueDropdown("@" + nameof(PlatformDropdownAttributeHelper) + "." + nameof(PlatformDropdownAttributeHelper.GetAllPlatforms) + "()")]
	public class PlatformDropdownAttribute : Attribute
	{
	}

	public static class PlatformDropdownAttributeHelper
	{
		public static IEnumerable<ValueDropdownItem<string>> GetAllPlatforms() => PlatformExt.GetAllPlatforms()
		   .Select(entry => new ValueDropdownItem<string>(entry.ToLabel(), entry));
	}
}
