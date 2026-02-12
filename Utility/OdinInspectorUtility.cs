using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

namespace Fusumity.Utility
{
	public static class OdinInspectorUtility
	{
		public static IEnumerable<ValueDropdownItem<string>> GetStringDropdownValues(this Type type)
		{
			yield return new ValueDropdownItem<string>("None", "");

			foreach (var fi in ReflectionUtility.GetConstantFieldInfos(type))
			{
				var value = (string)fi.GetValue(null);
				yield return new ValueDropdownItem<string>(value, value);
			}
		}
	}
}
