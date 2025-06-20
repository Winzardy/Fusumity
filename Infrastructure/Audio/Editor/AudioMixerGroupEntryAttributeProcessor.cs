using System;
using System.Collections.Generic;
using System.Reflection;
using Content;
using Sapientia;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Audio.Editor
{
	public class AudioMixerGroupEntryAttributeProcessor : OdinAttributeProcessor<AudioMixerGroupEntry>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(AudioMixerGroupEntry.useCustomPath):
					attributes.Add(new PropertySpaceAttribute(10));
					attributes.Add(new VerticalGroupAttribute("1"));
					attributes.Add(new InfoBoxAttribute($"@{nameof(AudioMixerGroupEntryAttributeProcessor)}." +
						$"{nameof(GetMessageCustomPath)}($property)", $"@{nameof(AudioMixerGroupEntryAttributeProcessor)}." +
						$"{nameof(IsVisibleCustomMixerPath)}($property)"));
					break;
				case nameof(AudioMixerGroupEntry.customMixerPath):
					attributes.Add(new VerticalGroupAttribute("1"));
					attributes.Add(new ShowIfAttribute(nameof(AudioMixerGroupEntry.useCustomPath)));
					break;

				case nameof(AudioMixerGroupEntry.icon):
				case nameof(AudioMixerGroupEntry.priority):
					attributes.Add(new EnableIfAttribute(nameof(AudioMixerGroupEntry.configurable)));
					break;

				case nameof(AudioMixerGroupEntry.useCustomVolumeExposedParameterName):
					attributes.Add(new PropertySpaceAttribute(10));
					attributes.Add(new VerticalGroupAttribute("2"));
					attributes.Add(new InfoBoxAttribute($"@{nameof(AudioMixerGroupEntryAttributeProcessor)}." +
						$"{nameof(GetMessageCustomVolumeExposedParameterName)}($property)",
						$"@{nameof(AudioMixerGroupEntryAttributeProcessor)}." +
						$"{nameof(IsVisibleCustomVolumeExposedParameterName)}($property)"));
					break;

				case nameof(AudioMixerGroupEntry.customVolumeExposedParameterName):
					attributes.Add(new VerticalGroupAttribute("2"));
					attributes.Add(new ShowIfAttribute(nameof(AudioMixerGroupEntry.useCustomVolumeExposedParameterName)));
					break;
			}
		}

		public static string GetMessageCustomPath(InspectorProperty property)
		{
			if (property.ParentValueProperty.ParentValueProperty.ValueEntry.WeakSmartValue is IIdentifiable identifiable)
				return "Текущий путь: " + identifiable.Id;

			return string.Empty;
		}

		public static bool IsVisibleCustomMixerPath(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioMixerGroupEntry entry)
			{
				if (property.ParentValueProperty.ParentValueProperty.ValueEntry.WeakSmartValue is IUniqueContentEntry<AudioMixerGroupEntry>
				    _)
					return !entry.useCustomPath;

				return false;
			}

			return false;
		}

		public static string GetMessageCustomVolumeExposedParameterName(InspectorProperty property)
		{
			if (property.ParentValueProperty.ParentValueProperty.ValueEntry.WeakSmartValue is IIdentifiable identifiable)
				return "Текущий путь: " + AudioMixerGroupEntry.GetExposedParameterName(identifiable.Id);

			return string.Empty;
		}

		public static bool IsVisibleCustomVolumeExposedParameterName(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioMixerGroupEntry entry)
			{
				if (property.ParentValueProperty.ParentValueProperty.ValueEntry.WeakSmartValue is IUniqueContentEntry<AudioMixerGroupEntry>
				    _)
					return !entry.useCustomVolumeExposedParameterName;

				return false;
			}

			return false;
		}
	}
}
