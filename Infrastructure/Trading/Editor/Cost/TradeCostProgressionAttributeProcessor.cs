#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia.Extensions.Reflection;
using Sapientia.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Trading.Editor
{
	public class TradeCostProgressionAttributeProcessor : TradeCostAttributeProcessor<TradeCostProgression>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(TradeCostProgression.schemeSource):
					attributes.Add(new LabelTextAttribute("Progress Source"));
					break;
				case nameof(TradeCostProgression.scheme):
					attributes.Add(new ShowIfAttribute(nameof(TradeCostProgression.ShowSchemeEditor)));
					break;

				case nameof(TradeCostProgression.schemeReference):
					attributes.Add(new HideIfAttribute(nameof(TradeCostProgression.ShowSchemeEditor)));
					break;
			}

			if (!IsInsideCollection(parentProperty))
				attributes.Add(new IndentAttribute(-1));
		}

		private bool IsInsideCollection(InspectorProperty? property)
		{
			while (property != null)
			{
				if (property.Parent?.ChildResolver is IOrderedCollectionResolver)
					return true;

				property = property.Parent;
			}

			return false;
		}
	}

	public class TradeCostProgressionStageAttributeProcessor : OdinAttributeProcessor<TradeCostProgressionStage>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));

			switch (member.Name)
			{
				case nameof(TradeCostProgressionStage.useOverrideCondition):
					attributes.Add(new LabelTextAttribute("Use Override Increment Condition"));
					attributes.Add(new ShowIfAttribute("@TradeCostProgressionStageAttributeProcessor.ShowIf($property)"));
					break;

				case nameof(TradeCostProgressionStage.overrideCondition):
					attributes.Add(new ShowIfAttribute("@TradeCostProgressionStageAttributeProcessor.ShowIfCondition($property)"));
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
					.ParentValueProperty?.ValueEntry.WeakSmartValue is TradeCostProgression progression)
			{
				return progression.schemeSource == TradeProgressionSchemeSource.Local;
			}

			return true;
		}

		public static bool ShowIfCondition(InspectorProperty property)
		{
			if (property.ParentValueProperty?.ValueEntry.WeakSmartValue is not TradeCostProgressionStage stage)
				return false;

			if (!stage.useOverrideCondition)
				return false;

			if (property
					.ParentValueProperty? // Stage
					.ParentValueProperty? // Array
					.ParentValueProperty? // ContentEntry
					.ParentValueProperty?.ValueEntry.WeakSmartValue is TradeCostProgression progression)
			{
				return progression.schemeSource == TradeProgressionSchemeSource.Local;
			}

			return true;
		}
	}
}
