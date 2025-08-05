using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace ZenoTween.Editor
{
	public class AnimationTweenAttributeProcessor : OdinAttributeProcessor<AnimationTween>
	{
		private const float SPACE = 0;
		private const float ORDER = 99;
		private const string GROUP = "AnimationTweenGroup";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
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
					break;

				case nameof(AnimationTween.repeat):
					attributes.Add(new PropertyOrderAttribute(ORDER + 2));
					attributes.Add(new MinimumAttribute(-1));
					var showIf = $"@{nameof(AnimationTweenAttributeProcessor)}." +
						$"{nameof(IsLoop)}($property)";
					attributes.Add(new InfoBoxAttribute(
						"Данный твин зациклен! Остановка твина будет при уничтожении верстки или через код!", InfoMessageType.Info,
						showIf));
					attributes.Add(new VerticalGroupAttribute($"{GROUP}/Vertical"));
					break;

				case nameof(AnimationTween.repeatType):
					attributes.Add(new HideIfAttribute(nameof(AnimationTween.repeat), 0));
					attributes.Add(new PropertyOrderAttribute(ORDER + 3));
					attributes.Add(new IndentAttribute());
					attributes.Add(new LabelTextAttribute("Type"));
					attributes.Add(new VerticalGroupAttribute($"{GROUP}/Vertical"));
					break;

				case nameof(AnimationTween.lifetimeByParent):
					attributes.Add(new VerticalGroupAttribute($"{GROUP}/Vertical"));
					attributes.Add(new PropertyOrderAttribute(ORDER + 4));
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
						Icon = SdfIconType.PlayFill,
						IconAlignment = IconAlignment.LeftEdge
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
						Icon = SdfIconType.StopFill,
						IconAlignment = IconAlignment.LeftEdge
					});
					break;

				case "duration":
					attributes.Add(new UnitAttribute(Units.Second));
					attributes.Add(new MinimumAttribute(0));
					break;

				case nameof(AnimationSequence.timeScale):
					attributes.Add(new VerticalGroupAttribute($"{GROUP}/Vertical"));
					attributes.Add(new PropertyOrderAttribute(ORDER + 4));
					break;
			}
		}

		public static bool IsLoop(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is AnimationTween tween)
				return tween.IsLoop && !tween.lifetimeByParent;

			return false;
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			var boxGroupAttribute = new BoxGroupAttribute("BoxGroup");
			boxGroupAttribute.ShowLabel = false;
			attributes.Add(boxGroupAttribute);
		}
	}
}
