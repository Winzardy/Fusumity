using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Targeting.Editor
{
	public class CountryEntryAttributeProcessor : OdinAttributeProcessor<CountryEntry>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(CountryEntry.code):
					attributes.Add(new CountryDropdownAttribute());
					attributes.Add(new HideLabelAttribute());
					break;
				case nameof(CountryEntry.name):
					attributes.Add(new HideInInspector());
					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new InlinePropertyAttribute());
		}
	}
}
