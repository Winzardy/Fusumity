using DG.Tweening;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.ArrowsMove, CategoryPath = CATEGORY_PATH)]
	public class MoveAnchoredPositionTween : AnimationTween
	{
		public RectTransform rectTransform;

		[Space]
		[InlineToggle(nameof(useStartValue), "From")]
		public Vector3 endValue = Vector3.zero;
		public bool useStartValue = false;
		[ShowIf(nameof(useStartValue))]
		public Vector3 startValue;

		[Space]
		public float duration = 0.5f;

		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			var tween = rectTransform
				.DOAnchorPos(endValue, duration)
				.SetEase(ease);

			if (useStartValue)
				tween.From(startValue);

			return tween;
		}

		protected override void OnValidate(GameObject owner)
		{
			if (rectTransform == null)
				owner.TryGetComponent(out rectTransform);
		}
	}
}
