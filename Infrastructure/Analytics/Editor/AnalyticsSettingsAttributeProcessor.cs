using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Analytics.Editor
{
	public class AnalyticsSettingsAttributeProcessor : OdinAttributeProcessor<AnalyticsSettings>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(AnalyticsSettings.disableAggregators):
					var valuesGetter = $"@{nameof(AnalyticsSettingsAttributeProcessor)}.{nameof(GetAllSchedulers)}()";
					var dropdown = new ValueDropdownAttribute(valuesGetter)
					{
						IsUniqueList = true
					};
					attributes.Add(dropdown);
					break;
			}
		}

		public static IEnumerable GetAllSchedulers()
		{
			var types = ReflectionUtility.GetAllTypes<AnalyticsAggregator>(includeSelf: false);
			return types.Select(x => new ValueDropdownItem(x.Name, x.FullName));
		}
	}
}
