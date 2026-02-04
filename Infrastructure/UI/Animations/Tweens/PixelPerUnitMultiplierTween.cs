using DG.Tweening;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoTween.Participant.Tweens.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.SquareHalf, CategoryPath = UIAnimationTweenConstants.TWEEN_CATEGORY_PATH)]
	public class PixelPerUnitMultiplierTween : AnimationTween
	{
		public Image image;

		[Space]
		[InlineToggle(nameof(useStartValue), "From")]
		public float endValue = 1;
		public bool useStartValue = false;
		[ShowIf(nameof(useStartValue))]
		public float startValue;

		[Space]
		public float duration = 0.5f;
		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			if (useStartValue)
			{
				image.pixelsPerUnitMultiplier = startValue;
			}

			var tween = DOTween.To(
				() => image.pixelsPerUnitMultiplier,
				x =>
				{
					image.pixelsPerUnitMultiplier = x;
				},
				endValue,
				duration)

				.SetEase(ease);

			return tween;
		}

		protected override void OnValidate(GameObject owner)
		{
			if (image == null)
				owner.TryGetComponent(out image);
		}
	}
}
