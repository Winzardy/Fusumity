using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace UI.Editor
{
	public class LayoutEntryAttributeProcessor : OdinAttributeProcessor<UILayoutEntry>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case "_layoutReference":
					attributes.Add(new PropertyOrderAttribute(-1));
					break;
				case nameof(UILayoutEntry.automationMode):
					attributes.Add(new BoxGroupAttribute("Automation", false));
					break;
				case nameof(UILayoutEntry.autoDestroyDelayMs):
					attributes.Add(new ShowIfAttribute($"@{nameof(LayoutEntryAttributeProcessor)}." +
						$"{nameof(HaveAutoDestroy)}($property)"));
					attributes.Add(new LabelTextAttribute("Delay"));
					attributes.Add(new UnitAttribute(Units.Millisecond));
					attributes.Add(new TimeFromMsSuffixLabelAttribute());
					attributes.Add(new BoxGroupAttribute("Automation/Auto Destroy", centerLabel: true));
					//attributes.Add(new TabGroupAttribute("Automation/Auto Destroy"));

					break;
			}
		}

		public static bool HaveAutoDestroy(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is UILayoutEntry entry)
				return entry.automationMode.HasFlag(LayoutAutomationMode.AutoDestroy);

			return false;
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			attributes.Add(new HideLabelAttribute());
		}
	}
}
