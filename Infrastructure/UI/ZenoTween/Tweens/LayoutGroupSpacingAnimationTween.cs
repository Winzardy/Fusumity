using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Sapientia;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.ArrowsCollapse,
		CategoryPath = CATEGORY_PATH)]
	public class LayoutGroupSpacingAnimationTween : AnimationTween
	{
		private RectTransform _cacheRectTransform;

		public HorizontalOrVerticalLayoutGroup group;

		public float to;
		public Toggle<float> from;

		[Space]
		public float duration = 0.5f;

		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			var tween = SpacingTween()
				.SetEase(ease);

			if (from)
				tween.From(from);

			return tween;
		}
		private TweenerCore<float, float, FloatOptions> SpacingTween()
		{
			var tween = DOTween.To(
				GetGroupSpacing,
				SetGroupSpacing,
				to,
				duration
			);
			return tween.SetTarget(group);

			float GetGroupSpacing() => group.spacing;
			void SetGroupSpacing(float spacing)
			{
				group.spacing = spacing;
				_cacheRectTransform ??= group.transform as RectTransform;
				LayoutRebuilder.ForceRebuildLayoutImmediate(_cacheRectTransform);
			}
		}
	}
}
