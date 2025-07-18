using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Utility;
using Localization;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace InAppPurchasing.Editor
{
	public class IAPProductEntryAttributeProcessor : OdinAttributeProcessor<IAPProductEntry>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));

			switch (member.Name)
			{
				case nameof(IAPProductEntry.customId):
					attributes.Add(new ShowIfAttribute(nameof(IAPProductEntry.useCustomId)));
					break;

				case nameof(IAPProductEntry.titleLocKey):
					attributes.Add(new SpaceAttribute());
					attributes.Add(new LocKeyAttribute());
					break;

				case nameof(IAPProductEntry.descriptionLocKey):
					attributes.Add(new LocKeyAttribute());
					break;
			}
		}
	}
}
