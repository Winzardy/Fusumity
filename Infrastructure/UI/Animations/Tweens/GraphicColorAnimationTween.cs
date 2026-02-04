using DG.Tweening;
using Fusumity.Attributes.Odin;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoTween.Participant.Tweens.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.Flower1, CategoryPath = UIAnimationTweenConstants.TWEEN_CATEGORY_PATH)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Tweens.UI",
		sourceAssembly: "UI")]
	public class GraphicColorAnimationTween : AnimationTween
	{
		public Graphic graphic;

		[InlineToggle(nameof(useStartColor), "From")]
		public Color color;
		public bool useStartColor = false;
		[ShowIf(nameof(useStartColor))]
		public Color startColor;

		public bool alpha = true;

		[Space]
		public float duration = 0.5f;

		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			if (!alpha)
				color = color.WithAlpha(graphic.color.a);

			var tween = graphic
			   .DOColor(color, duration)
			   .SetEase(ease);

			if (useStartColor)
			{
				if (!alpha)
					startColor.WithAlpha(graphic.color.a);

				tween.From(startColor);
			}

			return tween;
		}

		protected override void OnValidate(GameObject owner)
		{
			if (!graphic)
				graphic = owner.GetComponentInChildren<Graphic>();
		}
	}
}
