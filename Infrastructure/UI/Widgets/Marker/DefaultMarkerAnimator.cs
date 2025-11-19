using System;
using System.Collections.Generic;
using DG.Tweening;

namespace UI
{
	public class DefaultMarkerAnimator<TArgs> : BaseWidgetAnimator<UIMarkerLayout, UIMarker<TArgs>>
	{
		protected override void OnFill(Dictionary<string, Func<Sequence>> keyToSequenceFactory)
		{
			//TODO: проблема что он переопределяет, нужно подумать как решить кейс когда мы хотим разные режимы смешивания, как у Opening и Closing
			keyToSequenceFactory[WidgetAnimationType.MARKER_ENABLING] = CreateEnablingSequence;
			keyToSequenceFactory[WidgetAnimationType.MARKER_DISABLING] = CreateDisablingSequence;
		}

		protected override void OnCreateOpeningSequence(ref Sequence sequence)
		{
			if (!_layout.canvasGroup)
				return;

			sequence ??= DOTween.Sequence();
			sequence.Join(ShowingTween());
		}

		protected override void OnCreateClosingSequence(ref Sequence sequence)
		{
			if (!_layout.canvasGroup)
				return;

			sequence ??= DOTween.Sequence();
			sequence.Join(HidingTween());
		}

		private Sequence CreateEnablingSequence()
		{
			if (!_layout.canvasGroup)
				return null;

			return DOTween.Sequence()
			   .Join(ShowingTween());
		}

		private Sequence CreateDisablingSequence()
		{
			if (!_layout.canvasGroup)
				return null;

			return DOTween.Sequence()
			   .Join(HidingTween());
		}

		private Tween ShowingTween()
		{
			var duration = _widget.Enable ? _layout.showingDuration * (1 - _layout.canvasGroup.alpha) : 0;
			return _layout.canvasGroup.DOFade(_widget.Enable ? 1 : 0, duration);
		}

		private Tween HidingTween()
		{
			var duration = _layout.hidingDuration * _layout.canvasGroup.alpha;
			return _layout.canvasGroup.DOFade(0, duration);
		}
	}
}
