using DG.Tweening;
using Fusumity.Utility;
using UnityEngine;

namespace Fusumity.MVVM.UI
{
	public class UIAttentionIndicatorView : UIView<IStatefulViewModel, UIAttentionIndicatorLayout>
	{
		private Tween _tween;
		private Vector2 _originalPos;

		private bool _animate;

		public UIAttentionIndicatorView(UIAttentionIndicatorLayout layout, bool animate = true) : base(layout)
		{
			_animate = animate;
			_originalPos = layout.rectTransform.anchoredPosition;

			layout.SetActive(false);
		}

		protected override void OnDispose()
		{
			_tween?.Kill();
		}

		protected override void OnUpdate(IStatefulViewModel viewModel)
		{
			if (_animate)
			{
				PlayFloatAnimation();
			}

			UpdateActiveState();
			viewModel.ActiveStateChanged += HandleActiveStateChanged;
		}

		protected override void OnClear(IStatefulViewModel viewModel)
		{
			viewModel.ActiveStateChanged -= HandleActiveStateChanged;
		}

		protected override void OnNullViewModel()
		{
			Reset();
		}

		private void HandleActiveStateChanged(bool isActive)
		{
			UpdateActiveState();
		}

		private void UpdateActiveState()
		{
			_layout.SetActive(ViewModel != null && ViewModel.IsActive);
		}

		public override void Reset()
		{
			_tween.Kill();

			ClearViewModel();
			_layout.SetActive(false);
		}

		private void PlayFloatAnimation()
		{
			_tween.Kill();
			_layout.rectTransform.anchoredPosition = _originalPos;

			_tween = _layout.rectTransform
				.DOAnchorPosY(_layout.rectTransform.anchoredPosition.y + 10, 1f)
				.SetLoops(-1, LoopType.Yoyo)
				.SetEase(Ease.InOutSine);
		}
	}
}
