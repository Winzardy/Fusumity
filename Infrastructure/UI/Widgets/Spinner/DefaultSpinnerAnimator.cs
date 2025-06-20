using System;
using System.Collections.Generic;
using DG.Tweening;

namespace UI
{
	public class DefaultSpinnerAnimator : BaseWidgetAnimator<UISpinnerLayout>
	{
		protected override void OnFill(Dictionary<string, Func<Sequence>> keyToSequenceFactory)
		{
			keyToSequenceFactory[WidgetAnimationType.SPINNING] = CreateSpinnerSequence;
		}

		private Sequence CreateSpinnerSequence()
		{
			Sequence sequence = null;

			_layout.loopSequence?.Participate(ref sequence);

			if (sequence != null)
			{
				sequence.SetAutoKill(false);
				sequence.SetLoops(-1, LoopType.Incremental);
			}

			return sequence;
		}
	}
}
