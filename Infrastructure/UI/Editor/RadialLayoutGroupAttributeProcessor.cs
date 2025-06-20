using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace UI.Editor
{
	public class RadialLayoutGroupAttributeProcessor : OdinAttributeProcessor<RadialLayoutGroup>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case "m_Padding":
				case "m_ChildAlignment":
					attributes.Add(new HideInInspector());
					break;
			}
		}
	}
}
