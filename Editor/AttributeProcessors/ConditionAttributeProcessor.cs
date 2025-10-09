using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Editor.Utility;
using Sapientia.Conditions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class ConditionAttributeProcessor : ShowMonoScriptForReferenceAttributeProcessor<Condition>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(Condition.invert):
					//attributes.Add(new BoxGroupAttribute(Condition.BOX_GROUP, false));
					attributes.Add(new HorizontalGroupAttribute(Condition.GROUP, 23, marginRight: 2));
					attributes.Add(new LabelTextAttribute("!"));
					attributes.Add(new LabelWidthAttribute(8));
					attributes.Add(new TooltipAttribute("Инвертировать результат"));
					break;

				case nameof(CollectionCondition.mode):
					attributes.Add(new HorizontalGroupAttribute(Condition.GROUP));
					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			var color = Color.Lerp(Color.blue, Color.white, 0.83f);
			attributes.Add(new GUIColorAttribute(color.r, color.g, color.b));
			attributes.Add(new ColorCardBoxAttribute(
				Color.black.r,
				Color.black.g,
				Color.black.b,
				0.2f));
		}
	}
}
