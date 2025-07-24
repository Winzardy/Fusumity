using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEngine;

namespace Fusumity.Editor
{
	public class FolderPathAttributeProcessor : OdinAttributeProcessor<FolderPath>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(FolderPath.path):
					attributes.Add(new FolderPathAttribute());
					attributes.Add(new HideLabelAttribute());
					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			attributes.Add(new InlinePropertyAttribute());

			base.ProcessSelfAttributes(property, attributes);
		}
	}
}
