using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace UI.Editor
{
	public class BooleanStateSwitcherCombinerAttributeProcessor : OdinAttributeProcessor<BooleanStateSwitcherCombiner>
	{
		private const string GROUP = nameof(BooleanStateSwitcherCombiner);
		private const string CARD_GROUP = GROUP + "/Card";
		private const string CONTENT_GROUP = CARD_GROUP + "/Content";
		private const string A_GROUP = CONTENT_GROUP + "/A";
		private const string B_GROUP = CONTENT_GROUP + "/B";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member,
			List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case "_invert":
					attributes.Add(new PropertySpaceAttribute(4));
					AddInvertAttributes(attributes, GROUP);
					break;

				case "_invertA":
					AddContentAttributes(attributes);
					AddInvertAttributes(attributes, A_GROUP);
					break;

				case "_a":
					AddContentAttributes(attributes);
					attributes.Add(new HorizontalGroupAttribute(A_GROUP));
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new DisableStateSwitcherInlineEditorAttribute());
					break;

				case "_operator":
					attributes.Add(new HideLabelAttribute());
					AddContentAttributes(attributes, 65);
					break;

				case "_invertB":
					AddContentAttributes(attributes);
					AddInvertAttributes(attributes, B_GROUP);
					break;

				case "_b":
					AddContentAttributes(attributes);
					attributes.Add(new HorizontalGroupAttribute(B_GROUP));
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new DisableStateSwitcherInlineEditorAttribute());
					break;
			}
		}

		private static void AddContentAttributes(List<Attribute> attributes, int width = 0)
		{
			attributes.Add(new HorizontalGroupAttribute(GROUP));
			attributes.Add(new DarkCardBoxAttribute(CARD_GROUP));
			attributes.Add(new HorizontalGroupAttribute(CONTENT_GROUP, width));
		}

		private static void AddInvertAttributes(List<Attribute> attributes, string group)
		{
			attributes.Add(new HorizontalGroupAttribute(group, 24, marginRight: 2));
			attributes.Add(new LabelTextAttribute("!"));
			attributes.Add(new LabelWidthAttribute(8));
			attributes.Add(new TooltipAttribute("Инвертировать результат"));
		}
	}
}
