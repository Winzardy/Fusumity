using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Audio.Editor
{
	public class BasePointerAudioEventTriggerAttributeProcessor : OdinAttributeProcessor<BasePointerAudioEventTrigger>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);
			switch (member.Name)
			{
				case nameof(BasePointerAudioEventTrigger.selectable):
					attributes.Add(new SpaceAttribute());
					break;

				case nameof(BasePointerAudioEventTrigger.onlyInteractable):
					attributes.Add(new ShowIfAttribute(nameof(BasePointerAudioEventTrigger.selectable)));
					break;

				case nameof(BasePointerAudioEventTrigger.useCustomAudioEventForNonInteractable):
					attributes.Add(new ShowIfAttribute($"@{nameof(BasePointerAudioEventTriggerAttributeProcessor)}." +
						$"{nameof(ShowUseCustomAudioEventForNonInteractableEditor)}($property)"));
					break;

				case nameof(BasePointerAudioEventTrigger.customAudioEvent):
					attributes.Add(new ShowIfAttribute($"@{nameof(BasePointerAudioEventTriggerAttributeProcessor)}." +
						$"{nameof(ShowNonInteractableAudioEventEditor)}($property)"));
					break;
			}
		}

		public static bool ShowUseCustomAudioEventForNonInteractableEditor(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is BasePointerAudioEventTrigger entry)
				return entry.selectable && !entry.onlyInteractable;

			return false;
		}

		public static bool ShowNonInteractableAudioEventEditor(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is BasePointerAudioEventTrigger entry)
				return !entry.onlyInteractable && entry.useCustomAudioEventForNonInteractable;

			return false;
		}
	}
}
