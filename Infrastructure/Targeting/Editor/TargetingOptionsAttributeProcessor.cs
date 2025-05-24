using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Targeting.Editor
{
	public class AppOptionsAttributeProcessor : OdinAttributeProcessor<ProjectInfo>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(ProjectInfo.buildNumber):
				case nameof(ProjectInfo.identifier):
				case nameof(ProjectInfo.version):
					attributes.Add(new HideInInspector());
					break;
			}
		}
	}
}
