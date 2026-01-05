using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Sapientia;
using Sirenix.OdinInspector.Editor;

namespace Fusumity.Editor
{
	public class WeightableAttributeProcessor : OdinAttributeProcessor<IWeightable>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case "weight":
					attributes.Add(new DarkCardBoxAttribute());
					break;
			}
		}

	}
}
