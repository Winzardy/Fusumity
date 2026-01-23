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
	public class AudioEventRequestAttributeProcessor : OdinAttributeProcessor<AudioEventRequest>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (parentProperty.Attributes.HasAttribute<AllowLoopAttribute>())
			{
				var attribute = parentProperty.Attributes.GetAttribute<AllowLoopAttribute>();

				if (attribute.Loop)
				{
					if (member.Name == nameof(AudioEventRequest.repeat))
						attributes.Add(new HideIfAttribute(nameof(AudioEventRequest.loop)));
				}
			}
			else
			{
				if (member.Name == nameof(AudioEventRequest.loop))
				{
					attributes.Add(new HideInInspector());
				}
			}

			switch (member.Name)
			{
				case nameof(AudioEventRequest.id):
					attributes.Add(new ContentReferenceAttribute(typeof(AudioEventConfig), inlineEditor: false));
					break;

				case nameof(AudioEventRequest.repeat):
					attributes.Add(new MinimumAttribute(1));
					break;

				case nameof(AudioEventRequest.rerollOnRepeat):
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioEventRequestAttributeProcessor)}." +
						$"{nameof(ShowRerollOnRepeat)}($property)"));
					break;

				case nameof(AudioEventRequest.fadeIn):
				case nameof(AudioEventRequest.fadeOut):
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
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventRequest args)
				return args.repeat > 1 || args.loop;

			return true;
		}
	}
}
