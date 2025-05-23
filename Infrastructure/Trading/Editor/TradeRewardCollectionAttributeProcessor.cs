using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Trading.Editor
{
	public class TradeRewardCollectionAttributeProcessor : OdinAttributeProcessor<TradeRewardCollection>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(TradeRewardCollection.items):
					var typeSelectorSettingsAttribute = new TypeSelectorSettingsAttribute
					{
						FilterTypesFunction = nameof(TradeRewardCollection.Filter),
					};
					attributes.Add(typeSelectorSettingsAttribute);
					attributes.Add(new IndentAttribute(-1));
					break;
			}
		}
	}
}
