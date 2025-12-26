using System;
using System.Collections.Generic;
using Sapientia;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Fusumity.Editor
{
	public class CollectionEvaluatorAttributeProcessor : OdinAttributeProcessor<IList<IEvaluator>>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			TypeSelectorSettingsAttribute typeSelectorSettingsAttribute;
			if (EvaluatorAttributeProcessor.TopSemanticAncestorIsCondition(property))
			{
				typeSelectorSettingsAttribute = new TypeSelectorSettingsAttribute
				{
					FilterTypesFunction =
						$"@{nameof(EvaluatorAttributeProcessor)}.{nameof(EvaluatorAttributeProcessor.FilterByConditionRoot)}($type, $property)",
					ShowNoneItem = false,
				};
			}
			else
			{
				typeSelectorSettingsAttribute = new TypeSelectorSettingsAttribute
				{
					FilterTypesFunction =
						$"@{nameof(EvaluatorAttributeProcessor)}.{nameof(EvaluatorAttributeProcessor.Filter)}($type, $property)",
					ShowNoneItem = false,
				};
			}

			attributes.Add(typeSelectorSettingsAttribute);
		}
	}
}
