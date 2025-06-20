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
	public class AudioEventEntryAttributeProcessor : OdinAttributeProcessor<AudioEventEntry>
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
				case nameof(AudioEventEntry.mixer):

					attributes.Add(new PropertySpaceAttribute(0, 5));
					attributes.Add(new ContentReferenceAttribute(typeof(AudioMixerGroupEntry)));
					break;

				case nameof(AudioEventEntry.playMode):
					attributes.Add(new DisableIfAttribute(nameof(AudioEventEntry.EditorIsPlay)));
					attributes.Add(new BoxGroupAttribute(boxGroup, false));
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioEventEntryAttributeProcessor)}." +
						$"{nameof(ShowPlayMode)}($property)"));

					attributes.Add(new HorizontalGroupAttribute(horizontalGroup));
					attributes.Add(new VerticalGroupAttribute(verticalGroup));

					break;

				case nameof(AudioEventEntry.priority):
					attributes.Add(new SpaceAttribute());
					attributes.Add(new LabeledPropertyRangeAttribute(AudioEventEntry.MIN_PRIORITY, AudioEventEntry.MAX_PRIORITY,
						"High", "Low"));
					break;

				case nameof(AudioEventEntry.stereoPan):
					attributes.Add(new LabeledPropertyRangeAttribute(AudioEventEntry.MIN_STEREO_PAN, AudioEventEntry.MAX_STEREO_PAN,
						"Left", "Right"));
					break;

				case nameof(AudioEventEntry.selection):
					attributes.Add(new DisableIfAttribute(nameof(AudioEventEntry.EditorIsPlay)));
					attributes.Add(new VerticalGroupAttribute(verticalGroup));
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioEventEntryAttributeProcessor)}." +
						$"{nameof(ShowSelection)}($property)"));
					break;

				case nameof(AudioEventEntry.selectionRange):
					attributes.Add(new DisableIfAttribute(nameof(AudioEventEntry.EditorIsPlay)));
					attributes.Add(new VerticalGroupAttribute(verticalGroup));
					attributes.Add(new IndentAttribute());
					attributes.Add(new LabelTextAttribute("Range"));
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioEventEntryAttributeProcessor)}." +
						$"{nameof(ShowSelectionRange)}($property)"));
					attributes.Add(new PropertyRangeAttribute(1, $"@{nameof(AudioEventEntryAttributeProcessor)}." +
						$"{nameof(GetMax)}($property)"));
					break;

				case nameof(AudioEventEntry.sequenceType):
					attributes.Add(new DisableIfAttribute(nameof(AudioEventEntry.EditorIsPlay)));
					attributes.Add(new VerticalGroupAttribute(verticalGroup));
					attributes.Add(new IndentAttribute());
					attributes.Add(new LabelTextAttribute("Type"));
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioEventEntryAttributeProcessor)}." +
						$"{nameof(ShowSequenceType)}($property)"));
					break;

				case nameof(AudioEventEntry.PlayEditor):
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

				case nameof(AudioEventEntry.StopEditor):
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

				case nameof(AudioEventEntry.tracks):
					attributes.Add(new DisableIfAttribute(nameof(AudioEventEntry.EditorIsPlay)));
					attributes.Add(new PropertySpaceAttribute(2, 0));
					attributes.Add(new BoxGroupAttribute(boxGroup));
					break;

				case nameof(AudioEventEntry.timeScaledPitch):
				case nameof(AudioEventEntry.isSpatial):
					attributes.Add(new PropertySpaceAttribute(5, 0));
					break;

				case nameof(AudioEventEntry.spatial):
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new EnableIfAttribute(nameof(AudioEventEntry.isSpatial)));
					break;
			}
		}

		public static bool ShowSelectionRange(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventEntry entry)
				return ShowSelectionRange(entry);

			return false;
		}

		public static bool ShowSelectionRange(AudioEventEntry entry)
		{
			if (!ShowSelection(entry))
				return false;

			if (entry.tracks.Length <= 2)
				return false;

			return entry.selection != SelectionMode.None;
		}

		public static bool ShowSelection(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventEntry entry)
				return ShowSelection(entry);

			return false;
		}

		public static bool ShowSelection(AudioEventEntry entry) => ShowPlayMode(entry);

		public static bool ShowPlayMode(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventEntry entry)
				return ShowPlayMode(entry);

			return false;
		}

		public static bool ShowPlayEditor(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventEntry entry)
				return ShowPlayMode(entry) && !entry.EditorIsPlay;

			return false;
		}

		public static bool ShowStopEditor(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventEntry entry)
				return ShowPlayMode(entry) && entry.EditorIsPlay;

			return false;
		}

		public static bool ShowPlayMode(AudioEventEntry entry)
		{
			if (entry.tracks.IsNullOrEmpty())
				return false;

			return entry.tracks.Length > 1;
		}

		public static bool ShowSequenceType(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventEntry entry)
				return ShowSequenceType(entry);

			return false;
		}

		public static bool ShowSequenceType(AudioEventEntry entry)
		{
			if (!ShowPlayMode(entry))
				return false;

			if (entry.selection == SelectionMode.None)
				return entry.playMode == AudioPlayMode.Sequence;

			if (entry.selectionRange <= 1)
				return false;

			return entry.playMode == AudioPlayMode.Sequence;
		}

		public static int GetMax(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioEventEntry entry)
				return entry.tracks.Length - 1;

			return 1;
		}
	}
}
