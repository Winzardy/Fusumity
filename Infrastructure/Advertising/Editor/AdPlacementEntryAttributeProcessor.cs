using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Advertising.Editor
{
	public class AdPlacementEntryAttributeProcessor : OdinAttributeProcessor<AdPlacementEntry>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));

			switch (member.Name)
			{
				case nameof(AdPlacementEntry.usageLimit):
					attributes.Add(new SpaceAttribute());
					break;

				case nameof(AdPlacementEntry.integrationTrack):
					attributes.Add(new SpaceAttribute());
					attributes.Add(new PropertyOrderAttribute(100));
					break;

				case nameof(AdPlacementEntry.customName):
					attributes.Add(new ShowIfAttribute(nameof(AdPlacementEntry.useCustomName)));
					break;
			}
		}
	}
}
