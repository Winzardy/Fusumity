using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector.Editor;
using Trading.Advertising;

namespace Advertising.Editor
{
	public class RewardedAdTradeCostAttributeProcessor : OdinAttributeProcessor<RewardedAdTradeCost>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);
			switch (member.Name)
			{
				case nameof(RewardedAdTradeCost.count):
					attributes.Add(new MinimumAttribute(1));
					break;
			}
		}
	}
}
