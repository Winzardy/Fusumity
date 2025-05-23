using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace UI.Editor
{

	public class ButtonTransitionEntryAttributeProcessor : OdinAttributeProcessor<ButtonTransitionState>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(ButtonTransitionState.type):
					attributes.Add(new ButtonTransitionDropdownAttribute());
					attributes.Add(new HideLabelAttribute());
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
