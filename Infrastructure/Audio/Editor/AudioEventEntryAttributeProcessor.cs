using System;
using System.Collections.Generic;
using System.Reflection;
using Content;
using Fusumity.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEngine;

namespace Audio.Editor
{
	public class AudioEventEntryAttributeProcessor : OdinAttributeProcessor<AudioEventConfig>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			const int BUTTON_SIZE_WIDTH = 90;

			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			var boxGroup = "box";
			var horizontalGroup = $"{boxGroup}/horizontal";
			var verticalGroup = $"{horizontalGroup}/vertical";
			switch (member.Name)
			{
				case nameof(AudioEventConfig.mixer):

					attributes.Add(new PropertySpaceAttribute(0, 5));
					attributes.Add(new ContentReferenceAttribute(typeof(AudioMixerGroupEntry)));
					break;

				case nameof(AudioEventConfig.playMode):
					attributes.Add(new DisableIfAttribute(nameof(AudioEventConfig.EditorIsPlay)));
					attributes.Add(new BoxGroupAttribute(boxGroup, false));
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioEventEntryAttributeProcessor)}." +
						$"{nameof(ShowPlayMode)}($property)"));

					attributes.Add(new HorizontalGroupAttribute(horizontalGroup));
					attributes.Add(new VerticalGroupAttribute(verticalGroup));

					break;

				case nameof(AudioEventConfig.priority):
					attributes.Add(new SpaceAttribute());
					attributes.Add(new LabeledPropertyRangeAttribute(AudioEventConfig.MIN_PRIORITY, AudioEventConfig.MAX_PRIORITY,
						"High", "Low"));
					break;

				case nameof(AudioEventConfig.stereoPan):
					attributes.Add(new LabeledPropertyRangeAttribute(AudioEventConfig.MIN_STEREO_PAN, AudioEventConfig.MAX_STEREO_PAN,
						"Left", "Right"));
					break;

				case nameof(AudioEventConfig.selection):
					attributes.Add(new DisableIfAttribute(nameof(AudioEventConfig.EditorIsPlay)));
					attributes.Add(new VerticalGroupAttribute(verticalGroup));
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioEventEntryAttributeProcessor)}." +
						$"{nameof(ShowSelection)}($property)"));
					break;

				case nameof(AudioEventConfig.selectionRange):
					attributes.Add(new DisableIfAttribute(nameof(AudioEventConfig.EditorIsPlay)));
					attributes.Add(new VerticalGroupAttribute(verticalGroup));
					attributes.Add(new IndentAttribute());
					attributes.Add(new LabelTextAttribute("Range"));
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioEventEntryAttributeProcessor)}." +
						$"{nameof(ShowSelectionRange)}($property)"));
					attributes.Add(new PropertyRangeAttribute(1, $"@{nameof(AudioEventEntryAttributeProcessor)}." +
						$"{nameof(GetMax)}($property)"));
					break;

				case nameof(AudioEventConfig.sequenceType):
					attributes.Add(new DisableIfAttribute(nameof(AudioEventConfig.EditorIsPlay)));
					attributes.Add(new VerticalGroupAttribute(verticalGroup));
					attributes.Add(new IndentAttribute());
					attributes.Add(new LabelTextAttribute("Type"));
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioEventEntryAttributeProcessor)}." +
						$"{nameof(ShowSequenceType)}($property)"));
					break;

				case nameof(AudioEventConfig.PlayEditor):
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioEventEntryAttributeProcessor)}." +
						$"{nameof(ShowPlayEditor)}($property)"));

					attributes.Add(new HorizontalGroupAttribute(horizontalGroup, BUTTON_SIZE_WIDTH));

					attributes.Add(new ButtonAttribute("Play", ButtonStyle.FoldoutButton)
					{
						DisplayParameters = true,
						Icon = SdfIconType.PlayFill,
						IconAlignment = IconAlignment.LeftEdge
					});
					break;

				case nameof(AudioEventConfig.StopEditor):
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioEventEntryAttributeProcessor)}." +
						$"{nameof(ShowStopEditor)}($property)"));

					attributes.Add(new HorizontalGroupAttribute(horizontalGroup, BUTTON_SIZE_WIDTH));
					attributes.Add(new PropertySpaceAttribute(2));
					attributes.Add(new ButtonAttribute("Stop", ButtonStyle.FoldoutButton)
					{
						DisplayParameters = true,
						Icon = SdfIconType.StopFill,
						IconAlignment = IconAlignment.LeftEdge
					});
					break;

				case nameof(AudioEventConfig.tracks):
					attributes.Add(new DisableIfAttribute(nameof(AudioEventConfig.EditorIsPlay)));
					attributes.Add(new PropertySpaceAttribute(2, 0));
					attributes.Add(new BoxGroupAttribute(boxGroup));
					break;

				case nameof(AudioEventConfig.timeScaledPitch):
				case nameof(AudioEventConfig.isSpatial):
					attributes.Add(new PropertySpaceAttribute(5, 0));
					break;

				case nameof(AudioEventConfig.spatial):
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new EnableIfAttribute(nameof(AudioEventConfig.isSpatial)));
					break;
			}
		}

		public static bool ShowSelectionRange(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventConfig entry)
				return ShowSelectionRange(entry);

			return false;
		}

		public static bool ShowSelectionRange(AudioEventConfig config)
		{
			if (!ShowSelection(config))
				return false;

			if (config.tracks.Length <= 2)
				return false;

			return config.selection != SelectionMode.None;
		}

		public static bool ShowSelection(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventConfig entry)
				return ShowSelection(entry);

			return false;
		}

		public static bool ShowSelection(AudioEventConfig config) => ShowPlayMode(config);

		public static bool ShowPlayMode(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventConfig entry)
				return ShowPlayMode(entry);

			return false;
		}

		public static bool ShowPlayEditor(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventConfig entry)
				return ShowPlayMode(entry) && !entry.EditorIsPlay;

			return false;
		}

		public static bool ShowStopEditor(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventConfig entry)
				return ShowPlayMode(entry) && entry.EditorIsPlay;

			return false;
		}

		public static bool ShowPlayMode(AudioEventConfig config)
		{
			if (config.tracks.IsNullOrEmpty())
				return false;

			return config.tracks.Length > 1;
		}

		public static bool ShowSequenceType(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventConfig entry)
				return ShowSequenceType(entry);

			return false;
		}

		public static bool ShowSequenceType(AudioEventConfig config)
		{
			if (!ShowPlayMode(config))
				return false;

			if (config.selection == SelectionMode.None)
				return config.playMode == AudioPlayMode.Sequence;

			if (config.selectionRange <= 1)
				return false;

			return config.playMode == AudioPlayMode.Sequence;
		}

		public static int GetMax(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventConfig entry)
				return entry.tracks.Length - 1;

			return 1;
		}
	}
}
