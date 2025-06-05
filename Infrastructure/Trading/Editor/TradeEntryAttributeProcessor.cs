using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Editor;
using Fusumity.Editor.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Trading.Editor
{
	public class TradeEntryAttributeProcessor : OdinAttributeProcessor<TradeEntry>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(TradeEntry.reward):
					attributes.Add(new TitleGroupAttribute("Reward", "Что получаем за сделку", alignment: TitleAlignments.Split));
					break;

				case nameof(TradeEntry.cost):
					attributes.Add(new TitleGroupAttribute("Cost", "Что отдаем за сделку", alignment: TitleAlignments.Split));
					break;
			}

			switch (member.Name)
			{
				case nameof(TradeEntry.reward):
				case nameof(TradeEntry.cost):
					attributes.Add(new HideLabelAttribute());
					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new PropertySpaceAttribute(-4));
		}
	}

	public class ColorTintDealReceivableDrawer : ColorTintDrawer<TradeReward>
	{
		protected override Color Color => TradeReward.COLOR;
	}

	public class ColorTintDealPayableDrawer : ColorTintDrawer<TradeCost>
	{
		protected override Color Color => TradeCost.COLOR;
	}

	public class TradeCostAttributeProcessor : ShowMonoScriptForReferenceAttributeProcessor<TradeCost>
	{
	}

	public class TradeRewardAttributeProcessor : ShowMonoScriptForReferenceAttributeProcessor<TradeReward>
	{
	}
}
