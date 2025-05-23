using System;
using System.Collections.Generic;
using System.Reflection;
using Content;
using Fusumity.Attributes;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Audio.Editor
{
	public class AudioEventTriggerArgsAttributeProcessor : OdinAttributeProcessor<AudioEventTriggerArgs>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (parentProperty.Attributes.HasAttribute<AudioEventTriggerSupportAttribute>())
			{
				var attribute = parentProperty.Attributes.GetAttribute<AudioEventTriggerSupportAttribute>();

				if (attribute.Loop)
				{
					if (member.Name == nameof(AudioEventTriggerArgs.repeat))
						attributes.Add(new HideIfAttribute(nameof(AudioEventTriggerArgs.loop)));
				}
			}
			else
			{
				if (member.Name == nameof(AudioEventTriggerArgs.loop))
				{
					attributes.Add(new HideInInspector());
				}
			}

			switch (member.Name)
			{
				case nameof(AudioEventTriggerArgs.id):
					attributes.Add(new ContentReferenceAttribute(typeof(AudioEventEntry), foldout: false));
					break;

				case nameof(AudioEventTriggerArgs.repeat):
					attributes.Add(new MinimumAttribute(1));
					break;

				case nameof(AudioEventTriggerArgs.rerollOnRepeat):
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioEventTriggerArgsAttributeProcessor)}." +
						$"{nameof(ShowRerollOnRepeat)}($property)"));
					break;

				case nameof(AudioEventTriggerArgs.fadeIn):
				case nameof(AudioEventTriggerArgs.fadeOut):
					attributes.Add(new UnitParentAttribute(Units.Second));
					attributes.Add(new MinimumAttribute(0));
					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			attributes.Add(new FoldoutContainerAttribute());
		}

		public static bool ShowRerollOnRepeat(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventTriggerArgs args)
				return args.repeat > 1 || args.loop;

			return true;
		}
	}
}
