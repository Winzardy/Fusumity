using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace UI.Editor
{
	public class SortingOverriderAttributeProcessor : OdinAttributeProcessor<SortingOverrider>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(SortingOverrider.canvas):
					attributes.Add(new HideInInspector());
					break;
			}
		}
	}
}
