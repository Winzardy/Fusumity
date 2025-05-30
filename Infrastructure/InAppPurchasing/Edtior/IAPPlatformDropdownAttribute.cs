using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sirenix.OdinInspector;

namespace InAppPurchasing.Editor
{
	[Conditional("UNITY_EDITOR")]
	[IncludeMyAttributes]
	[ValueDropdown("@" + nameof(IAPPlatformDropdownAttributeHelper) + "." + nameof(IAPPlatformDropdownAttributeHelper.GetAll) +
		"()")]
	public class IAPPlatformDropdownAttribute : Attribute
	{
	}

	public static class IAPPlatformDropdownAttributeHelper
	{
		public static IEnumerable<ValueDropdownItem<string>> GetAll() =>
			IAPPlatformExtensions.GetAll().Select(pair => new ValueDropdownItem<string>(IAPPlatformExtensions.GetLabel(pair.platform), pair.platform));
	}
}
