using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Editor.Utility;
using Sapientia.Conditions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class BlackboardConditionAttributeProcessor : ShowMonoScriptForReferenceAttributeProcessor<BlackboardCondition>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(InvertableBlackboardCondition.invert):
					//attributes.Add(new BoxGroupAttribute(Condition.BOX_GROUP, false));
					attributes.Add(new HorizontalGroupAttribute(BlackboardCondition.GROUP, 23, marginRight: 2));
					attributes.Add(new LabelTextAttribute("!"));
					attributes.Add(new LabelWidthAttribute(8));
					attributes.Add(new TooltipAttribute("Инвертировать результат"));
					break;

				case nameof(CollectionCondition.mode):
					attributes.Add(new HorizontalGroupAttribute(BlackboardCondition.GROUP));
					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			var color = Color.Lerp(Color.blue, Color.white, 0.83f);

			if (attributes.FirstOrDefault(a => a is GUIColorAttribute) is GUIColorAttribute colorAttribute)
			{
				colorAttribute.Color = color;
			}
			else
			{
				attributes.Add(new GUIColorAttribute(color.r, color.g, color.b));
			}

			color = Color.Lerp(Color.blue, Color.black, 0.83f);
			if (attributes.FirstOrDefault(a => a is ColorCardBoxAttribute) is ColorCardBoxAttribute box)
			{
				box.R = color.r;
				box.G = color.g;
				box.B = color.b;
				box.A = 0.2f;
			}
			else
			{
				attributes.Add(new ColorCardBoxAttribute(
					color.r,
					color.g,
					color.b,
					0.2f));
			}
		}
	}
}
