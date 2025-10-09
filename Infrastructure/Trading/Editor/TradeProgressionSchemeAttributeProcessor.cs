#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Utility;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Trading.Editor
{
	public class TradeProgressionSchemeAttributeProcessor : OdinAttributeProcessor<TradeProgressionScheme>
	{
		public static readonly Dictionary<InspectorProperty, GUIContent> propertyToGUIContent = new();

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));
			switch (member.Name)
			{
				case nameof(TradeProgressionScheme.type):
					if (propertyToGUIContent.TryGetValue(parentProperty, out var content))
					{
						if (!content.text.IsNullOrEmpty())
							attributes.Add(new LabelTextAttribute(content.text));
						else
							attributes.Add(new HideLabelAttribute());

						if (!content.tooltip.IsNullOrEmpty())
							attributes.Add(new TooltipAttribute(content.tooltip));
					}
					else
						attributes.Add(new HideLabelAttribute());

					break;

				case nameof(TradeProgressionScheme.schedule):
				case nameof(TradeProgressionScheme.realTime):
					attributes.Add(new HideIfAttribute(nameof(TradeProgressionScheme.type), TradeProgressionResetType.None));
					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			attributes.Add(new DarkCardBoxAttribute());
			var guiContent = new GUIContent(property.Label);
			propertyToGUIContent[property] = guiContent;

			if (attributes.GetAttribute<HideLabelAttribute>() != null)
				guiContent.text = string.Empty;
			else if (attributes.GetAttribute<LabelTextAttribute>() != null)
				guiContent.text = attributes.GetAttribute<LabelTextAttribute>().Text;

			if (attributes.GetAttribute<TooltipAttribute>() != null)
				guiContent.tooltip = attributes.GetAttribute<TooltipAttribute>().tooltip;

			attributes.Add(new HideLabelAttribute());
			attributes.RemoveAll(attr => attr is LabelTextAttribute);
		}
	}
}
