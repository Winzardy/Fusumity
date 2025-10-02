using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Editor.Utility;
using Sapientia.Conditions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
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
					attributes.Add(new HorizontalGroupAttribute(23, marginRight:2));
					attributes.Add(new LabelTextAttribute("!"));
					attributes.Add(new LabelWidthAttribute(8));
					attributes.Add(new TooltipAttribute("Инвертировать результат"));
					break;

				case "mode":
					attributes.Add(new HorizontalGroupAttribute());
					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new BoxGroupAttribute());
		}
	}
}
