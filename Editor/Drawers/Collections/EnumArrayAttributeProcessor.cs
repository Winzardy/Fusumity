using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Collections;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Fusumity.Editor.Drawers.Collections
{
	public class EnumArrayAttributeProcessor : OdinAttributeProcessor<IEnumArray>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);
			attributes.Add(new LabelTextAttribute(parentProperty.Label.text));
		}
	}
}
