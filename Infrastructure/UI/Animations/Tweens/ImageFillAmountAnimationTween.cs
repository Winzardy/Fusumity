using System;
using DG.Tweening;
using Fusumity.Attributes;
using Sapientia;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoTween.Participant.Tweens.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.SquareHalf)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Tweens.UI",
		sourceAssembly: "UI")]
	public class ImageFillAmountAnimationTween : AnimationTween
	{
		public Image image;

		[PropertyRange(0, 1)]
		public float to;

		[PropertyRangeParent(0, 1)]
		public Toggle<float> from;

		[Space]
		public float duration = 0.5f;

		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			var tween = image
			   .DOFillAmount(to, duration)
			   .SetEase(ease);

			if (from.enable)
				tween.From(from.value);

			return tween;
		}

		protected override void OnValidate(GameObject owner)
		{
			if (!image)
				image = owner.GetComponentInChildren<Image>();
		}
	}
}
