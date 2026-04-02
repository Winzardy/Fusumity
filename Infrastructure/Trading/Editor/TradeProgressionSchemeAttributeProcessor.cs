#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Editor;
using Sapientia.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Trading.Editor
{
	public class TradeProgressionSchemeAttributeProcessor : OdinAttributeProcessor<TradeProgressionScheme>
	{
		// protected override string ValueFieldName => nameof(TradeProgressionScheme.type);

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));

			switch (member.Name)
			{
				case nameof(TradeProgressionScheme.condition):
					attributes.Add(new LabelTextAttribute("Increment Condition"));
					break;
				case nameof(TradeProgressionScheme.type):
					attributes.Add(new LabelTextAttribute("Reset Type"));
					break;
				case nameof(TradeProgressionScheme.schedule):
					attributes.Add(new HideIfAttribute(nameof(TradeProgressionScheme.type), TradeProgressionScheduleResetType.None));
					break;
			}

			attributes.Add(new DarkCardBoxAttribute());
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new HideLabelAttribute());

		}
	}
}
