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
	public class AudioSpatialEntryAttributeProcessor : OdinAttributeProcessor<AudioSpatialScheme>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			const int BUTTON_SIZE_WIDTH = 90;

			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(AudioSpatialScheme.spread):
					attributes.Add(new RangeAttribute(AudioSpatialScheme.SPREAD_MIN, AudioSpatialScheme.SPREAD_MAX));
					break;

				case nameof(AudioSpatialScheme.dopplerLevel):
					attributes.Add(new RangeAttribute(AudioSpatialScheme.DOPPLER_LEVEL_MIN, AudioSpatialScheme.DOPPLER_LEVEL_MAX));
					break;

				case nameof(AudioSpatialScheme.customRolloffCurve):
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioSpatialEntryAttributeProcessor)}." +
						$"{nameof(IsCustomRolloffCurve)}($property)"));
					break;

				case nameof(AudioSpatialScheme.spatialBlend):
					attributes.Add(new LabeledPropertyRangeAttribute(0, 1, "2D", "3D"));
					break;

				case nameof(AudioSpatialScheme.distance):
					attributes.Add(new UnitParentAttribute(Units.Meter));
					attributes.Add(new MinimumParentAttribute(0));
					break;
			}
		}

		public static bool IsCustomRolloffCurve(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioSpatialScheme entry)
				return entry.rolloffMode == AudioRolloffMode.Custom;

			return false;
		}
	}
}
