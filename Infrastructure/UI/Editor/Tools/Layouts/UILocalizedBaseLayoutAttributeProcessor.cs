using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace UI.Editor
{
	public class UILocalizedBaseLayoutAttributeProcessor : OdinAttributeProcessor<UILocalizedBaseLayout>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			var target = parentProperty.ValueEntry.WeakSmartValue as UILocalizedBaseLayout;

			// if (target!.Label && ReferenceEquals(target.GetReflectionValue(member.Name), target!.Label))
			// 	attributes.Add(new VerticalGroupAttribute(nameof(UILocalizedBaseLayout)));

			switch (member.Name)
			{
				case nameof(UILocalizedBaseLayout.locInfo):
					attributes.Add(new IndentAttribute());
					attributes.Add(new LabelTextAttribute("Localize"));
					attributes.Add(new VerticalGroupAttribute(nameof(UILocalizedBaseLayout)));
					attributes.Add(
						new ShowIfAttribute($"@{nameof(UILocalizedBaseLayoutAttributeProcessor)}.{nameof(ShowLocInfo)}($property)"));
					attributes.Add(new PropertyOrderAttribute(999));
					break;
			}
		}

		public static bool ShowLocInfo(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is UILocalizedBaseLayout layout)
				return layout.Label;
			return false;
		}
	}
}
