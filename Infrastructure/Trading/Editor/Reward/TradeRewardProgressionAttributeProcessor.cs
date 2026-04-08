#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Survivor.Interop;
using UnityEngine;

namespace Trading.Editor
{
	public class TradeRewardProgressionAttributeProcessor : OdinAttributeProcessor<TradeRewardProgression>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));
			switch (member.Name)
			{
				case nameof(TradeRewardProgression.schemeSource):
					attributes.Add(new LabelTextAttribute("Progress Source"));
					break;
				case nameof(TradeRewardProgression.scheme):
					attributes.Add(new ShowIfAttribute(nameof(TradeRewardProgression.ShowSchemeEditor)));
					break;

				case nameof(TradeRewardProgression.schemeReference):
					attributes.Add(new HideIfAttribute(nameof(TradeCostProgression.ShowSchemeEditor)));
					break;

				case nameof(TradeRewardProgression.hide):
					attributes.Add(new ClientVisualGroupAttribute());
					break;
				case nameof(TradeRewardProgression.visual):
					attributes.Add(new ClientVisualGroupAttribute());
					attributes.Add(new HideIfAttribute(nameof(TradeRewardProgression.hide)));
					break;
			}
		}
	}

	public class TradeRewardProgressionStageAttributeProcessor : OdinAttributeProcessor<TradeRewardProgressionStage>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));

			switch (member.Name)
			{
				case nameof(TradeRewardProgressionStage.useOverrideCondition):
					attributes.Add(new LabelTextAttribute("Use Override Increment Condition"));
					attributes.Add(new ShowIfAttribute("@TradeRewardProgressionStageAttributeProcessor.ShowIf($property)"));

					break;

				case nameof(TradeRewardProgressionStage.overrideCondition):
					attributes.Add(new ShowIfAttribute("@TradeRewardProgressionStageAttributeProcessor.ShowIfCondition($property)"));

					attributes.Add(new LabelTextAttribute("Increment Condition"));
					break;
			}
		}

		public static bool ShowIf(InspectorProperty property)
		{
			if (property
					.ParentValueProperty? // Stage
					.ParentValueProperty? // Array
					.ParentValueProperty? // ContentEntry
					.ParentValueProperty?
					.ValueEntry.WeakSmartValue is TradeRewardProgression progression)
				return progression.schemeSource == TradeProgressionSchemeSource.Local;

			return true;
		}

		public static bool ShowIfCondition(InspectorProperty property)
		{
			if (property.ParentValueProperty?.ValueEntry.WeakSmartValue is not TradeRewardProgressionStage stage)
				return false;

			if (!stage.useOverrideCondition)
				return false;

			if (property
					.ParentValueProperty? // Stage
					.ParentValueProperty? // Array
					.ParentValueProperty? // ContentEntry
					.ParentValueProperty?.ValueEntry.WeakSmartValue is TradeRewardProgression progression)
			{
				return progression.schemeSource == TradeProgressionSchemeSource.Local;
			}

			return true;
		}
	}
}
