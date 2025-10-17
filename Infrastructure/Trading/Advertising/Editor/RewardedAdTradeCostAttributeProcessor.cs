using System;
using System.Collections.Generic;
using System.Reflection;
using Content;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector.Editor;
using Trading.Advertising;
using UnityEngine;

namespace Advertising.Editor
{
	public class RewardedAdTradeCostAttributeProcessor : OdinAttributeProcessor<RewardedAdTradeCost>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);
			switch (member.Name)
			{
				case nameof(RewardedAdTradeCost.group):
					attributes.Add(new ContextLabelAttribute("AdTokenGroup"));
					attributes.Add(new TooltipAttribute(
						"Нужно чтобы разделять выдачу билетиков по группам, например чтобы в одном месте не использовались допустимые билетики из другой группы"));
					break;
			}
		}
	}
}
