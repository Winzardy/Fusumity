using System;
using System.Collections.Generic;
using Fusumity.Editor.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Trading.Editor
{
	public class TradeRewardAttributeProcessor : ShowMonoScriptForReferenceAttributeProcessor<TradeReward>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			var typeSelectorSettingsAttribute = new TypeSelectorSettingsAttribute
			{
				ShowNoneItem = false
			};

			attributes.Add(typeSelectorSettingsAttribute);
		}
	}
}
