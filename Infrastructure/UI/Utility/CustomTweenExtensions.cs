using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine.UI;

namespace UI
{
	public static class CustomTweenExtensions
	{
		public static TweenerCore<float, float, FloatOptions> DOValue(this Scrollbar target, float endValue, float duration)
		{
			if (endValue > 1) endValue = 1;
			else if (endValue < 0) endValue = 0;
			TweenerCore<float, float, FloatOptions> t = DOTween.To(() => target.value, x => target.value = x, endValue, duration);
			t.SetTarget(target);
			return t;
		}

		public static TweenerCore<float, float, FloatOptions> DOSize(this Scrollbar target, float endValue, float duration)
		{
			if (endValue > 1) endValue = 1;
			else if (endValue < 0) endValue = 0;
			TweenerCore<float, float, FloatOptions> t = DOTween.To(() => target.size, x => target.size = x, endValue, duration);
			t.SetTarget(target);
			return t;
		}
	}
}
