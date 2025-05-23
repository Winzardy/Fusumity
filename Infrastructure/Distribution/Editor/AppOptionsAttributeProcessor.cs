using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Distribution.Editor
{
	public class AppOptionsAttributeProcessor : OdinAttributeProcessor<AppOptions>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(AppOptions.buildNumber):
				case nameof(AppOptions.identifier):
				case nameof(AppOptions.version):
					attributes.Add(new HideInInspector());
					break;
			}
		}
	}
}
