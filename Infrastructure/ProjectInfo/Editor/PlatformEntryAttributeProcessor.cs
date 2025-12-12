using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace ProjectInformation.Editor
{
	public class PlatformEntryAttributeProcessor : OdinAttributeProcessor<PlatformEntry>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(PlatformEntry.platform):
					attributes.Add(new PlatformDropdownAttribute());
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
