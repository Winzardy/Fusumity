using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Audio.Editor
{
	public class AudioSpatialEntryAttributeProcessor : OdinAttributeProcessor<AudioSpatialEntry>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			const int BUTTON_SIZE_WIDTH = 90;

			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(AudioSpatialEntry.spread):
					attributes.Add(new RangeAttribute(AudioSpatialEntry.SPREAD_MIN, AudioSpatialEntry.SPREAD_MAX));
					break;

				case nameof(AudioSpatialEntry.dopplerLevel):
					attributes.Add(new RangeAttribute(AudioSpatialEntry.DOPPLER_LEVEL_MIN, AudioSpatialEntry.DOPPLER_LEVEL_MAX));
					break;

				case nameof(AudioSpatialEntry.customRolloffCurve):
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioSpatialEntryAttributeProcessor)}." +
						$"{nameof(IsCustomRolloffCurve)}($property)"));
					break;

				case nameof(AudioSpatialEntry.spatialBlend):
					attributes.Add(new LabeledPropertyRangeAttribute(0, 1, "2D", "3D"));
					break;

				case nameof(AudioSpatialEntry.distance):
					attributes.Add(new UnitParentAttribute(Units.Meter));
					attributes.Add(new MinimumParentAttribute(0));
					break;
			}
		}

		public static bool IsCustomRolloffCurve(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioSpatialEntry entry)
				return entry.rolloffMode == AudioRolloffMode.Custom;

			return false;
		}
	}
}
