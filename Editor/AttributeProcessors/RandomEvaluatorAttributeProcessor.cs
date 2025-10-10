using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia.Evaluator;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class RandomEvaluatorAttributeProcessor : OdinAttributeProcessor<IRandomEvaluator>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);
			switch (member.Name)
			{
				case "min":
				case "max":
					attributes.Add(new HorizontalGroupAttribute("Random"));
					attributes.Add(new LabelWidthAttribute(30));
					break;
			}
		}
	}
}
