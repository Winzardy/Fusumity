using System;
using System.Collections.Generic;
using Sapientia;
using Sirenix.OdinInspector.Editor;

namespace Fusumity.Editor
{
	public class EvaluatorAutoRegistrationAttributeProcessor : OdinAttributeProcessor<IEvaluator>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			var type = property.ValueEntry.TypeOfValue;
			if (EvaluatorTypeRegistryUtility.TryGetKnownGenericPresentation(type, out var presentation))
				EvaluatorTypeRegistryUtility.Register(type, presentation);
		}
	}
}
