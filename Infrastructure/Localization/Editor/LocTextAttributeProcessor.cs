using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Localization.Editor
{
	public class LocTextAttributeProcessor : OdinAttributeProcessor<LocText>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(LocText.key):
					attributes.Add(new LocKeyAttribute());
					break;
				case nameof(LocText.composite):
				case nameof(LocText.tagToFunc):
				case nameof(LocText.defaultValue):
					attributes.Add(new HideInInspector());
					break;

			}
		}

	}
}
