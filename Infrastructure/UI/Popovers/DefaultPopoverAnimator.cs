using DG.Tweening;
using UnityEngine;

namespace UI
{
	public class DefaultPopoverAnimator : BaseWidgetAnimator<UIBaseLayout>
	{
		private const float OPENING_TIME = 0.5f;
		private const float CLOSING_TIME = 0.3f;

		protected override void OnCreateOpeningSequence(ref Sequence sequence)
		{
			sequence ??= DOTween.Sequence();

			// if (_layout.canvasGroup)
			// {
			// 	sequence.Join(_layout.canvasGroup
			// 	   .DOFade(1f, OPENING_TIME)
			// 	   .From(0f));
			// }
			//
			sequence.Join(_layout.rectTransform
			   .DOAnchorPosY(0f, OPENING_TIME)
			   .From(new Vector2(0, -100f))
			   .SetEase(Ease.OutBack));
		}

		protected override void OnCreateClosingSequence(ref Sequence sequence)
		{
			sequence ??= DOTween.Sequence();

			// if (_layout.canvasGroup)
			// {
			// 	sequence.Join(_layout.canvasGroup
			// 	   .DOFade(0f, CLOSING_TIME * 0.7f)
			// 	   .From(1f));
			// }
			//
			sequence.Join(_layout.rectTransform
			   .DOAnchorPosY(-200f, CLOSING_TIME)
			   .From(Vector2.zero)
			   .SetEase(Ease.OutCubic));
		}
	}
}
