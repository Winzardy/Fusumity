using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UI.Bridge;

namespace UI.Editor
{
	public class BridgeStateSwitcherAttributeProcessor : OdinAttributeProcessor<IBridgeStateSwitcher>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member,
			List<Attribute> attributes)
		{
			const string modeMemberName = "_mode";

			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case modeMemberName:
					attributes.Add(new PropertyOrderAttribute(-10));
					break;

				case "_single":
					attributes.Add(new ShowIfAttribute(modeMemberName, BridgeMode.Single));
					attributes.Add(new PropertyOrderAttribute(-10));
					attributes.Add(new PropertySpaceAttribute(0, 5));
					break;

				case "_group":
					attributes.Add(new ShowIfAttribute(modeMemberName, BridgeMode.Group));
					attributes.Add(new PropertyOrderAttribute(-10));
					attributes.Add(new PropertySpaceAttribute(0, 5));
					break;

				case "_dictionary":
					attributes.Add(new LabelTextAttribute("State Mapping (Input → Output)"));
					var dictDrawSettings = new DictionaryDrawerSettings
					{
						KeyLabel = "Input",
						ValueLabel = "Output"
					};
					attributes.Add(dictDrawSettings);
					break;
			}
		}
	}
}
