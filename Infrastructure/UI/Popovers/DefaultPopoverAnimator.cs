using DG.Tweening;
using UI.Popovers;

namespace UI
{
	public class DefaultPopoverAnimator<TPopover> : BaseWidgetAnimator<UIBaseCanvasGroupLayout, TPopover>
		where TPopover : UIWidget, IPopover
	{
		private const float OPENING_TIME = 0.5f;
		private const float CLOSING_TIME = 0.3f;

		protected override void OnCreateOpeningSequence(ref Sequence sequence)
		{
			sequence ??= DOTween.Sequence();

			if (_layout.canvasGroup)
			{
				sequence.Join(_layout.canvasGroup
				   .DOFade(1f, OPENING_TIME));
			}

			sequence.Join(_layout.rectTransform
			   .DOLocalMoveY(100, OPENING_TIME)
			   .SetEase(Ease.OutBack));
		}

		protected override void OnCreateClosingSequence(ref Sequence sequence)
		{
			sequence ??= DOTween.Sequence();

			if (_layout.canvasGroup)
			{
				sequence.Join(_layout.canvasGroup
				   .DOFade(0f, CLOSING_TIME));
			}

			sequence.Join(_layout.rectTransform
			   .DOLocalMoveY(25, CLOSING_TIME)
			   .SetEase(Ease.OutCubic));
		}
	}
}
