#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Editor;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Trading.Editor
{
	public class TradeProgressionSchemeAttributeProcessor : ValueWrapperOdinAttributeProcessor<TradeProgressionScheme>
	{
		protected override string ValueFieldName => nameof(TradeProgressionScheme.type);

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));
			switch (member.Name)
			{
				case nameof(TradeProgressionScheme.schedule):
					attributes.Add(new HideIfAttribute(nameof(TradeProgressionScheme.type), TradeProgressionResetType.None));
					break;
			}
		}
	}
}
