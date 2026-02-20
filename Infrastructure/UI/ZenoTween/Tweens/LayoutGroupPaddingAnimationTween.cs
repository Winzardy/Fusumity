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
	[TypeRegistryItem(Icon = SdfIconType.ArrowsMove,
		CategoryPath = CATEGORY_PATH)]
	public class LayoutGroupPaddingAnimationTween : AnimationTween
	{
		private RectTransform _cacheRectTransform;

		public HorizontalOrVerticalLayoutGroup group;

		public RectOffset to;

		[Space]
		public float duration = 0.5f;

		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			var tween = Tween()
				.SetEase(ease);

			return tween;
		}

		private Tween Tween()
		{
			return DOTween.To(
				GetPadding,
				SetPadding,
				to,
				duration
			);

			RectOffset GetPadding() => group.padding;
			void SetPadding(RectOffset padding)
			{
				group.padding = padding;
				_cacheRectTransform ??= group.transform as RectTransform;
				LayoutRebuilder.ForceRebuildLayoutImmediate(_cacheRectTransform);
			}
		}
	}
}
