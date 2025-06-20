using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes.Odin;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using RangeAttribute = UnityEngine.RangeAttribute;

namespace Audio.Editor
{
	public class AudioTrackEntryAttributeProcessor : OdinAttributeProcessor<AudioTrackEntry>
	{
		private static Color WEIGHT_COLOR = new(153 / 256f, 153 / 256f, 153 / 256f, 1);

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(AudioTrackEntry.delay):
					attributes.Add(new UnitAttribute(Units.Second));
					attributes.Add(new MinimumAttribute(0));

					break;

				case nameof(AudioTrackEntry.clipReference):
					attributes.Add(new PropertyOrderAttribute(0));
					attributes.Add(new VerticalGroupAttribute(AudioTrackEntry.PLAY_GROUP_EDITOR));
					attributes.Add(new EditorAudioPlayAttribute());
					break;

				case nameof(AudioTrackEntry.volume):
					attributes.Add(new RangeAttribute(0, 1));
					break;

				case nameof(AudioTrackEntry.pitch):
					const string ATTEMP_PITCH_MESSAGE =
						"Отрицательный <b>pitch</b> не поддерживается.";

					const string ATTEMP_PITCH_FULL_MESSAGE =
						"Отрицательный <b>pitch</b> поддерживается только для аудиоклипов, которые хранятся в несжатом формате или будут распакованы во время загрузки.";

					attributes.Add(new DetailedInfoBoxAttribute(ATTEMP_PITCH_MESSAGE,ATTEMP_PITCH_FULL_MESSAGE,InfoMessageType.Warning,
						$"@{nameof(AudioTrackEntryAttributeProcessor)}." +
						$"{nameof(ShowInfoBoxPitch)}($property)"));
					attributes.Add(new RangeAttribute(AudioTrackEntry.MIN_PITCH, AudioTrackEntry.MAX_PITCH));
					break;

				case nameof(AudioTrackEntry.weight):

					attributes.Add(new GUIColorAttribute($"@{nameof(AudioTrackEntryAttributeProcessor)}." +
						$"{nameof(GetWeightColorEditor)}($property)"));
					attributes.Add(new PropertyOrderAttribute(-1));
					attributes.Add(new PropertySpaceAttribute(0, 3));
					attributes.Add(new ShowIfAttribute($"@{nameof(AudioTrackEntryAttributeProcessor)}." +
						$"{nameof(ShowWeight)}($property)"));

					break;

				case "_normalizedTime":
					attributes.Add(new PropertyOrderAttribute(1));
					attributes.Add(new VerticalGroupAttribute(AudioTrackEntry.PLAY_GROUP_EDITOR));
					attributes.Add(new PropertySpaceAttribute(-7, -6));
					attributes.Add(new ShowIfAttribute(nameof(AudioTrackEntry.IsPlayEditor)));
					attributes.Add(new ProgressBarAttribute(0, 1)
					{
						DrawValueLabel = false, Height = 4, BackgroundColorGetter = "clear"
					});
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new EnableGUIAttribute());
					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			attributes.Add(new GUIColorAttribute($"@{nameof(AudioTrackEntryAttributeProcessor)}." +
				$"{nameof(GetColorEditor)}($property)"));
		}

		public static Color GetColorEditor(InspectorProperty property)
		{
			if (property.ValueEntry.WeakSmartValue is AudioTrackEntry entry)
				return entry.SelectedEditor.HasValue ? entry.SelectedEditor.Value ? Color.white : Color.gray.WithAlpha(0.25f) : Color.white;

			return Color.white;
		}

		public static Color GetWeightColorEditor(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioTrackEntry entry)
				return entry.SelectedEditor.HasValue
					? entry.SelectedEditor.Value ? WEIGHT_COLOR : Color.gray.WithAlpha(0.25f)
					: WEIGHT_COLOR;

			return WEIGHT_COLOR;
		}

		public static bool ShowWeight(InspectorProperty property)
		{
			if (property
				   .ParentValueProperty
				   .ParentValueProperty
				   .ParentValueProperty
				   .ValueEntry.WeakSmartValue is AudioEventEntry entry)
			{
				return ShowWeight(entry);
			}

			return false;
		}

		public static bool ShowInfoBoxPitch(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AudioTrackEntry entry)
				return entry.pitch < 0 && entry.clipReference.editorAsset.loadType is AudioClipLoadType.Streaming or AudioClipLoadType.CompressedInMemory;

			return false;
		}

		public static bool ShowWeight(AudioEventEntry entry) =>
			AudioEventEntryAttributeProcessor.ShowPlayMode(entry) && entry.selection == SelectionMode.Random;
	}

	public class EditorAudioPlayAttribute : Attribute
	{
	}
}
