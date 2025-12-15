using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace ProjectInformation.Editor
{
	public class AppOptionsAttributeProcessor : OdinAttributeProcessor<ProjectInfoConfig>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(ProjectInfoConfig.buildNumber):
				case nameof(ProjectInfoConfig.identifier):
				case nameof(ProjectInfoConfig.version):
					attributes.Add(new HideInInspector());
					break;
			}
		}
	}
}
