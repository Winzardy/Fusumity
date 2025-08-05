using DG.Tweening;

namespace UI.Windows
{
	public class DefaultWindowAnimator : BaseWidgetAnimator<UIBaseContainerLayout>
	{
		private const float OPENING_TIME = 0.5f;
		private const float CLOSING_TIME = 0.3f;

		protected override void OnCreateOpeningSequence(ref Sequence sequence)
			=> CreateOpeningSequence(_layout, ref sequence);

		protected override void OnCreateClosingSequence(ref Sequence sequence)
			=> CreateClosingSequence(_layout, ref sequence);

		public static void CreateOpeningSequence(UIBaseContainerLayout layout, ref Sequence sequence)
		{
			sequence ??= DOTween.Sequence();

			if (layout.canvasGroup)
			{
				sequence.Join(layout.canvasGroup
				   .DOFade(1, OPENING_TIME)
				   .From(0));
			}

			sequence.Join(layout.container
			   .DOScale(1, OPENING_TIME)
			   .From(1.1f)
			   .SetEase(Ease.OutBack));
		}

		public static void CreateClosingSequence(UIBaseContainerLayout layout, ref Sequence sequence)
		{
			sequence ??= DOTween.Sequence();

			if (layout.canvasGroup)
			{
				sequence.Join(layout.canvasGroup
				   .DOFade(0, CLOSING_TIME * 0.7f)
				   .From(1));
			}

			sequence.Join(layout.container
			   .DOScale(0.9f, CLOSING_TIME)
			   .From(1)
			   .SetEase(Ease.OutCubic));
		}
	}
}
