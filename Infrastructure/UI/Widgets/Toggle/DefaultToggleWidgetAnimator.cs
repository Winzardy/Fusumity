using System;
using System.Collections.Generic;
using DG.Tweening;

namespace UI
{
	public class DefaultToggleWidgetAnimator : BaseWidgetAnimator<UIToggleButtonLayout>
	{
		protected override void OnFill(Dictionary<string, Func<Sequence>> keyToSequenceFactory)
		{
			keyToSequenceFactory[WidgetAnimationType.TOGGLE_ENABLING] = CreateEnablingSequence;
			keyToSequenceFactory[WidgetAnimationType.TOGGLE_DISABLING] = CreateDisablingSequence;
		}

		private Sequence CreateEnablingSequence()
		{
			Sequence sequence = null;
			_layout.onSequence?.Participate(ref sequence, _layout);
			return sequence;
		}

		private Sequence CreateDisablingSequence()
		{
			Sequence sequence = null;
			_layout.offSequence?.Participate(ref sequence, _layout);
			return sequence;
		}
	}
}
