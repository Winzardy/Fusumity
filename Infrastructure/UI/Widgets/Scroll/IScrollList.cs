using System;
using UnityEngine;

namespace UI.Scroll
{
	public interface IScrollList<TItemArgs>
	{
		public TItemArgs[] Data { get; }

		public void TweenTo(int itemIndex,
			UIScrollLayout.TweenType tweenType = UIScrollLayout.TweenType.easeOutQuad,
			float time = 0.5f,
			Action onComplete = null,
			bool completeCachedTween = true);

		public event Action<UIScrollLayout, Vector2, float> Scrolled;
		public event Action<TItemArgs[], float> Updated;
	}
}
