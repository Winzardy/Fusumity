using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia;
using Sapientia.Evaluators;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

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
					attributes.Add(new HorizontalGroupAttribute(nameof(IRandomEvaluator)));
					attributes.Add(new LabelWidthAttribute(30));
					break;
			}
		}
	}
}
