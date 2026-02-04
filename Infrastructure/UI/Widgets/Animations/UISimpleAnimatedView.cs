using Fusumity.MVVM.UI;
using UnityEngine;
using ZenoTween.Utility;

namespace UI
{
	public class UISimpleAnimatedView : UIView<Vector2, UIAnimationSequenceLayout>
	{
		private AnimationSequencePlayer _animation;

		public UISimpleAnimatedView(UIAnimationSequenceLayout layout) : base(layout)
		{
			AddDisposable(_animation = new AnimationSequencePlayer(layout.animationSequence));
		}

		protected override void OnUpdate(Vector2 screenPos)
		{
			SetActive(true);
			_layout.rectTransform.position = screenPos;

			_animation.Play();
		}

		public override void Reset()
		{
			ClearViewModel();
			_animation.Stop();
			SetActive(false);
		}
	}
}
