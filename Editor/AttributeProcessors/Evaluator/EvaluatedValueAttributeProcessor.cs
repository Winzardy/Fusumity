using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia.Evaluators;
using Sirenix.OdinInspector.Editor;
using HideIfAttribute = Sirenix.OdinInspector.HideIfAttribute;
using ShowIfAttribute = Sirenix.OdinInspector.ShowIfAttribute;

namespace Fusumity.Editor
{
	public class EvaluatedValueAttributeProcessor : ValueWrapperOdinAttributeProcessor<IEvaluatedValue>
	{
		protected override string ValueFieldName => "value";
		protected override string SecondValueFieldName => "evaluator";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.Name == ValueFieldName)
			{
				attributes.Add(new HideIfAttribute(nameof(IEvaluatedValue.Evaluator), null, false));
			}

			if (member.Name == SecondValueFieldName)
			{
				attributes.Add(new ShowIfAttribute(nameof(IEvaluatedValue.Evaluator), null, false));
			}
		}
	}
}
