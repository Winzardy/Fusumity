using System;
using System.Collections.Generic;
using System.Reflection;
using Content;
using Fusumity.Attributes;
using Game.Trading.UI;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Trading.Editor
{
	public class TradeRewardRepresentableAttributeProcessor : OdinAttributeProcessor<ITradeRewardRepresentable>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case "visual":
					attributes.Add(new ContentReferenceAttribute(typeof(UIRewardVisualConfig)));
					attributes.Add(new TooltipAttribute("Добавляет собственную визуализацию награды.\n" +
						"Если не задано, визуализация наград продолжается с обходом вложенных наград"));
					attributes.Add(new PropertyOrderAttribute(1000));
					attributes.Add(new DarkCardBoxAttribute
					{
						Space = 5
					});
					break;
			}
		}
	}
}
