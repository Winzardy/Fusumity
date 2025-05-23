using System;
using System.Collections.Generic;
using DG.Tweening;
using Fusumity.Utility;

namespace UI
{
	public class DefaultProgressBarAnimator : BaseWidgetAnimator<UIProgressBarLayout, UIProgressBar>
	{
		protected override void OnFill(Dictionary<string, Func<Sequence>> keyToSequenceFactory)
		{
			keyToSequenceFactory[WidgetAnimationType.PROGRESS_BAR] = CreateSequence;
		}

		private Sequence CreateSequence()
		{
			var sequence = DOTween.Sequence();
			ref readonly var value = ref _widget.args;

			if (_layout.hideOutsideAnimation)
				sequence.PrependCallback(Show);

			switch (_layout.type)
			{
				case UIProgressBarLayout.Type.Image:

					sequence.Join(_layout.image
					   .DOFillAmount(value, _layout.animationDuration)
					   .SetEase(_layout.animationEase));

					break;

				case UIProgressBarLayout.Type.ScrollBar:
					sequence.Join(_layout.scrollBar
					   .DOSize(value, _layout.animationDuration)
					   .SetEase(_layout.animationEase));

					break;
			}

			if (_layout.hideOutsideAnimation)
				sequence.AppendCallback(Hide);

			return sequence;

			void Show()
			{
				switch (_layout.type)
				{
					case UIProgressBarLayout.Type.Image:
						_layout.image.SetActive(true);
						break;
					case UIProgressBarLayout.Type.ScrollBar:
						_layout.scrollBar.SetActive(true);
						break;
				}
			}

			void Hide()
			{
				switch (_layout.type)
				{
					case UIProgressBarLayout.Type.Image:
						_layout.image.SetActive(false);
						break;
					case UIProgressBarLayout.Type.ScrollBar:
						_layout.scrollBar.SetActive(false);
						break;
				}
			}
		}
	}
}
