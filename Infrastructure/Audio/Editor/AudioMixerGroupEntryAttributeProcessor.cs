using System;
using System.Collections.Generic;
using System.Reflection;
using Content;
using Sapientia;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Audio.Editor
{
	public class AudioMixerGroupEntryAttributeProcessor : OdinAttributeProcessor<AudioMixerGroupConfig>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(AudioMixerGroupConfig.useCustomPath):
					attributes.Add(new PropertySpaceAttribute(10));
					attributes.Add(new VerticalGroupAttribute("1"));
					attributes.Add(new InfoBoxAttribute($"@{nameof(AudioMixerGroupEntryAttributeProcessor)}." +
						$"{nameof(GetMessageCustomPath)}($property)", $"@{nameof(AudioMixerGroupEntryAttributeProcessor)}." +
						$"{nameof(IsVisibleCustomMixerPath)}($property)"));
					break;
				case nameof(AudioMixerGroupConfig.customMixerPath):
					attributes.Add(new VerticalGroupAttribute("1"));
					attributes.Add(new ShowIfAttribute(nameof(AudioMixerGroupConfig.useCustomPath)));
					break;

				case nameof(AudioMixerGroupConfig.icon):
				case nameof(AudioMixerGroupConfig.priority):
					attributes.Add(new EnableIfAttribute(nameof(AudioMixerGroupConfig.configurable)));
					break;

				case nameof(AudioMixerGroupConfig.useCustomVolumeExposedParameterName):
					attributes.Add(new PropertySpaceAttribute(10));
					attributes.Add(new VerticalGroupAttribute("2"));
					attributes.Add(new InfoBoxAttribute($"@{nameof(AudioMixerGroupEntryAttributeProcessor)}." +
						$"{nameof(GetMessageCustomVolumeExposedParameterName)}($property)",
						$"@{nameof(AudioMixerGroupEntryAttributeProcessor)}." +
						$"{nameof(IsVisibleCustomVolumeExposedParameterName)}($property)"));
					break;

				case nameof(AudioMixerGroupConfig.customVolumeExposedParameterName):
					attributes.Add(new VerticalGroupAttribute("2"));
					attributes.Add(new ShowIfAttribute(nameof(AudioMixerGroupConfig.useCustomVolumeExposedParameterName)));
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
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioMixerGroupConfig entry)
			{
				if (property.ParentValueProperty.ParentValueProperty.ValueEntry.WeakSmartValue is IUniqueContentEntry<AudioMixerGroupConfig>
				    _)
					return !entry.useCustomPath;

				return false;
			}

			return false;
		}

		public static string GetMessageCustomVolumeExposedParameterName(InspectorProperty property)
		{
			if (property.ParentValueProperty.ParentValueProperty.ValueEntry.WeakSmartValue is IIdentifiable identifiable)
				return "Текущий путь: " + AudioMixerGroupConfig.GetExposedParameterName(identifiable.Id);

			return string.Empty;
		}

		public static bool IsVisibleCustomVolumeExposedParameterName(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioMixerGroupConfig entry)
			{
				if (property.ParentValueProperty.ParentValueProperty.ValueEntry.WeakSmartValue is IUniqueContentEntry<AudioMixerGroupConfig>
				    _)
					return !entry.useCustomVolumeExposedParameterName;

				return false;
			}

			return false;
		}
	}
}
