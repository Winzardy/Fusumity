using DG.Tweening;

namespace UI.Screens
{
	public class DefaultScreenAnimator : BaseWidgetAnimator<UIBaseContainerLayout>
	{
		private const float OPENING_TIME = 0.5f;
		private const float CLOSING_TIME = 0.4f;

		protected override void OnCreateOpeningSequence(ref Sequence sequence)
		{
			sequence ??= DOTween.Sequence();

			if (_layout.canvasGroup)
			{
				sequence.Join(_layout.canvasGroup
				   .DOFade(1, OPENING_TIME)
				   .From(0));
			}

			sequence.Join(_layout.container
			   .DOScale(1, OPENING_TIME)
			   .From(1.1f));
		}

		protected override void OnCreateClosingSequence(ref Sequence sequence)
		{
			sequence ??= DOTween.Sequence();

			if (_layout.canvasGroup)
			{
				sequence.Join(_layout.canvasGroup
				   .DOFade(0, CLOSING_TIME)
				   .From(1));
			}

			sequence.Join(_layout.container
			   .DOScale(1.1f, CLOSING_TIME)
			   .From(1));
		}
	}
}
