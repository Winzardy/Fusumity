using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Localization.Editor
{
	public class TextLocalizationArgsAttributeProcessor : OdinAttributeProcessor<TextLocalizationArgs>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(TextLocalizationArgs.key):
					attributes.Add(new LocKeyAttribute());
					break;
				case nameof(TextLocalizationArgs.composite):
				case nameof(TextLocalizationArgs.tagsWithFunc):
				case nameof(TextLocalizationArgs.autoReturnToPool):
				case nameof(TextLocalizationArgs.defaultValue):
					attributes.Add(new HideInInspector());
					break;

			}
		}

	}
}
