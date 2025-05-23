using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sirenix.OdinInspector;

namespace UI.Editor
{
	[Conditional("UNITY_EDITOR")]
	[IncludeMyAttributes]
	[ValueDropdown("@" + nameof(ButtonTransitionDropdownAttributeHelper) + "." +
		nameof(ButtonTransitionDropdownAttributeHelper.GetAllTypes) + "()")]
	public class ButtonTransitionDropdownAttribute : Attribute
	{
	}

	public static class ButtonTransitionDropdownAttributeHelper
	{
		public static IEnumerable<ValueDropdownItem<int>> GetAllTypes() =>
			ButtonTransitionUtility.GetAll().Select(entry => new ValueDropdownItem<int>(entry.ToLabel(), entry));
	}
}
