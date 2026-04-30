using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Attributes.Odin;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZenoTween.Editor
{
	public class AnimationTweenAttributeProcessor : OdinAttributeProcessor<AnimationTween>
	{
		private const float SPACE = 0;
		private const float ORDER = 99;
		private const string GROUP = "AnimationTweenGroup";
		private const string DURATION_NAME = "duration";
		private const string EASE_NAME = "ease";

		private static readonly Dictionary<string, bool> EditorLoopStates = new();

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(AnimationSequence.participants):
					attributes.Add(new ListDrawerSettingsAttribute
					{
						OnTitleBarGUI = $"@{nameof(AnimationTweenAttributeProcessor)}.{nameof(DrawTimelineButton)}($property)"
					});
					break;

				case nameof(AnimationTween.type):

					attributes.Add(new PropertyOrderAttribute(ORDER));
					attributes.Add(new PropertySpaceAttribute(SPACE));

					attributes.Add(new HorizontalGroupAttribute(GROUP));
					attributes.Add(new VerticalGroupAttribute($"{GROUP}/Vertical"));

					attributes.Add(new EnableIfAttribute(nameof(AnimationTween.UseType)));
					break;

				case nameof(AnimationTween.delay):
					attributes.Add(new PropertyOrderAttribute(ORDER + 1));
					attributes.Add(new TimeFromMsSuffixLabelAttribute());
					attributes.Add(new UnitAttribute(Units.Second));
					attributes.Add(new MinimumAttribute(0));
					attributes.Add(new VerticalGroupAttribute($"{GROUP}/Vertical"));
					attributes.Add(new HideIfAttribute(nameof(AnimationTween.type), AnimationTween.Type.Immediate));

					if (parentProperty.ValueEntry.TypeOfValue == typeof(AnimationSequence))
					{
						attributes.Add(new InlineToggleAttribute(nameof(AnimationSequence.delayOnce), "Once")
						{
							showIf  = nameof(AnimationSequence.IsLoop),
							margins = 5
						});
					}

					break;

				case nameof(AnimationTween.speed):
					attributes.Add(new PropertyOrderAttribute(ORDER + 2));
					attributes.Add(new MinValueAttribute(0.01f));
					attributes.Add(new VerticalGroupAttribute($"{GROUP}/Vertical"));
					attributes.Add(new HideIfAttribute(nameof(AnimationTween.type), AnimationTween.Type.Immediate));

					break;

				case nameof(AnimationTween.repeat):
					attributes.Add(new PropertyOrderAttribute(ORDER + 3));
					attributes.Add(new MinValueAttribute(-1));
					var showIf = $"@{nameof(AnimationTweenAttributeProcessor)}." +
						$"{nameof(IsLoop)}($property)";
					attributes.Add(new InfoBoxAttribute(
						"Данный твин зациклен! Остановка твина будет при уничтожении верстки или через код!", InfoMessageType.Info,
						showIf));
					attributes.Add(new VerticalGroupAttribute($"{GROUP}/Vertical"));
					attributes.Add(new HideIfAttribute(nameof(AnimationTween.type), AnimationTween.Type.Immediate));

					break;

				case nameof(AnimationTween.repeatType):
					attributes.Add(new ShowIfAttribute(nameof(AnimationTween.UseRepeat)));

					attributes.Add(new PropertyOrderAttribute(ORDER + 4));
					attributes.Add(new IndentAttribute());
					attributes.Add(new LabelTextAttribute("Type"));
					attributes.Add(new VerticalGroupAttribute($"{GROUP}/Vertical"));

					break;

				case nameof(AnimationTween.lifetimeByParent):
					attributes.Add(new VerticalGroupAttribute($"{GROUP}/Vertical"));
					attributes.Add(new PropertyOrderAttribute(ORDER + 5));
					attributes.Add(new ShowIfAttribute(nameof(AnimationTween.IsLoop)));
					break;

				case nameof(AnimationTween.PlayTweenEditor):
					attributes.Add(new DisableInPlayModeAttribute());
					attributes.Add(new HideIfAttribute(nameof(AnimationTween.EditorTweenActive)));
					attributes.Add(new PropertyOrderAttribute(ORDER + 3));
					attributes.Add(new PropertySpaceAttribute(SPACE));

					attributes.Add(new HorizontalGroupAttribute(GROUP, AnimationTween.BUTTON_SIZE_WIDTH_EDITOR));
					attributes.Add(new ButtonAttribute("Play", ButtonStyle.FoldoutButton)
					{
						DisplayParameters = true,
						Icon              = SdfIconType.PlayFill,
						IconAlignment     = IconAlignment.LeftEdge
					});
					break;

				case nameof(AnimationTween.StopTweenEditor):
					attributes.Add(new DisableInPlayModeAttribute());
					attributes.Add(new ShowIfAttribute(nameof(AnimationTween.EditorTweenActive)));
					attributes.Add(new PropertyOrderAttribute(ORDER + 3));
					attributes.Add(new PropertySpaceAttribute(SPACE));

					attributes.Add(new HorizontalGroupAttribute(GROUP, AnimationTween.BUTTON_SIZE_WIDTH_EDITOR));
					attributes.Add(new ButtonAttribute("Stop", ButtonStyle.FoldoutButton)
					{
						DisplayParameters = true,
						Icon              = SdfIconType.StopFill,
						IconAlignment     = IconAlignment.LeftEdge
					});
					break;

				case DURATION_NAME:
					attributes.Add(new UnitAttribute(Units.Second));
					attributes.Add(new MinValueAttribute(0));
					attributes.Add(new HideIfAttribute(nameof(AnimationTween.type), AnimationTween.Type.Immediate));

					break;

				case EASE_NAME:
					attributes.Add(new HideIfAttribute(nameof(AnimationTween.type), AnimationTween.Type.Immediate));

					break;

				case nameof(AnimationTween.immediateType):
					attributes.Add(new VerticalGroupAttribute($"{GROUP}/Vertical"));
					attributes.Add(new PropertyOrderAttribute(ORDER + 10));
					attributes.Add(new LabelTextAttribute("Callback"));
					attributes.Add(new ShowIfAttribute(nameof(AnimationTween.type), AnimationTween.Type.Immediate));

					break;
				case nameof(AnimationSequence.timeScale):
					attributes.Add(new VerticalGroupAttribute($"{GROUP}/Vertical"));
					attributes.Add(new PropertyOrderAttribute(ORDER + 5));
					break;

				case nameof(AnimationSequence.delayOnce):
					attributes.Add(new HideInInspector());
					break;
			}
		}

		public static bool IsLoop(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AnimationTween tween)
				return tween.IsLoop && !tween.lifetimeByParent;

			return false;
		}

		private static void DrawTimelineButton(InspectorProperty property)
		{
			//TODO:
			return;

			if (property.Parent?.ValueEntry?.WeakSmartValue is not AnimationSequence sequence)
				return;

			if (!SirenixEditorGUI.ToolbarButton(SdfIconType.BarChartSteps))
				return;

			var root = property.SerializationRoot?.ValueEntry?.WeakSmartValue as Object;
			AnimationSequenceTimelineWindow.Open(sequence, root);
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			var boxGroupAttribute = new BoxGroupAttribute("BoxGroup");
			boxGroupAttribute.ShowLabel = false;
			attributes.Add(boxGroupAttribute);

			var method = "@" + nameof(AnimationTweenAttributeProcessor);
			method += "." + nameof(DrawEditorPreviewControls) + "($property)";
			attributes.Add(new OnInspectorGUIAttribute(method, append: false));
		}

		public static void DrawEditorPreviewControls(InspectorProperty property)
		{
			if (Application.isPlaying)
				return;

			if (property?.ValueEntry?.WeakSmartValue is not AnimationTween tween)
				return;

			var loop = GetLoopState(property);
			var rowHeight = Mathf.Max(9f, EditorGUIUtility.singleLineHeight * 0.64f);
			var rowRect = EditorGUILayout.GetControlRect(false, rowHeight);
			var playButtonRect = AlignRight(rowRect, 18f);
			var loopButtonRect = AlignRight(rowRect, 18f, 20f);
			var progressPosition = rowRect;
			progressPosition.y      += 1f;
			progressPosition.height -= 2f;
			progressPosition.width  =  Mathf.Max(0f, loopButtonRect.x - rowRect.x);

			if (tween.type != AnimationTween.Type.Immediate && GUI.Button(playButtonRect, GUIContent.none, GUIStyle.none))
			{
				if (!tween.EditorPreviewActive)
					tween.PlayEditor(tween.EditorReset, loop: loop);
				else
					tween.StopEditor();
			}

			if (GUI.Button(loopButtonRect, GUIContent.none, GUIStyle.none))
			{
				loop = !loop;
				SetLoopState(property, loop);
			}

			progressPosition.y      += 3f;
			progressPosition.x      += 2f;
			progressPosition.height =  3;
			SirenixEditorFields.ProgressBarField(progressPosition, GUIContent.none, tween.EditorTweenPosition, 0, tween.EditorTweenFullPosition, new ProgressBarConfig
			{
				BackgroundColor = ProgressBarConfig.Default.BackgroundColor,
				ForegroundColor = Color.white,
				DrawValueLabel  = false,
				Height          = 3
			});

			var originalColor = GUI.color;

			var playIconRect = playButtonRect;
			//playIconRect.y += 1f;
			if (tween.EditorPreviewActive)
			{
				playIconRect        = AlignRight(playIconRect, 11f, 3.5f);
				playIconRect.height = 11f;
				EditorIcons.Stop.Draw(playIconRect);
			}
			else
			{
				playIconRect        = AlignRight(playIconRect, 19f, 0f);
				playIconRect.height = 11f;
				GUI.color           = tween.type == AnimationTween.Type.Immediate ? originalColor.WithAlpha(0.25f) : originalColor;
				EditorIcons.Play.Draw(playIconRect);
				GUI.color = originalColor;
			}

			GUI.color = loop ? originalColor : originalColor.WithAlpha(0.45f);
			var loopIconRect = loopButtonRect;
			//	loopIconRect.y      += 1f;
			loopIconRect        = AlignRight(loopIconRect, 11f, -2f);
			loopIconRect.height = 11f;
			SdfIcons.DrawIcon(loopIconRect, SdfIconType.ArrowRepeat);
			GUI.color = originalColor;

			EditorGUIUtility.AddCursorRect(playButtonRect, MouseCursor.Link);
			EditorGUIUtility.AddCursorRect(loopButtonRect, MouseCursor.Link);
		}

		private static bool GetLoopState(InspectorProperty property)
		{
			return EditorLoopStates.GetValueOrDefault(property.Path);
		}

		private static void SetLoopState(InspectorProperty property, bool loop)
		{
			EditorLoopStates[property.Path] = loop;
		}

		private static Rect AlignRight(Rect rect, float width, float offset = 0)
		{
			rect.x     = rect.x + rect.width - width - offset;
			rect.width = width;
			return rect;
		}
	}
}
