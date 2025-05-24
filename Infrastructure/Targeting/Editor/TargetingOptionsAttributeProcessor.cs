using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Targeting.Editor
{
	public class AppOptionsAttributeProcessor : OdinAttributeProcessor<TargetingOptions>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(TargetingOptions.buildNumber):
				case nameof(TargetingOptions.identifier):
				case nameof(TargetingOptions.version):
					attributes.Add(new HideInInspector());
					break;
			}
		}
	}
}
