using System;
using System.Collections.Generic;
using DG.Tweening;
using Fusumity.MVVM.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class UIToggleBarView : UIView<IToggleBarViewModel, UIToggleBarLayout>
	{
		private UIToggleButtonsCollection _collection;

		public UIToggleBarView(UIToggleBarLayout layout, Func<IUIAnimator<UIToggleButtonLayout>> animatorFactory = null) : base(layout)
		{
			AddDisposable(_collection = new UIToggleButtonsCollection(layout, animatorFactory));

			if (layout.back != null)
				Subscribe(layout.back, HandleBackClicked, _layout.backButtonUniqueId, _layout.backButtonGroupId);
		}

		protected override void OnUpdate(IToggleBarViewModel viewModel)
		{
			SetActive(true);

			_collection.Update(viewModel.Buttons);
			viewModel.ButtonsChanged += HandleButtonsChanged;
		}

		protected override void OnClear(IToggleBarViewModel viewModel)
		{
			viewModel.ButtonsChanged -= HandleButtonsChanged;
		}

		protected override void OnNullViewModel()
		{
			SetActive(false);
		}

		private void HandleButtonsChanged()
		{
			_collection.Update(ViewModel.Buttons);
		}

		private void HandleBackClicked()
		{
			ViewModel?.ClickBack();
		}
	}

	public class UIToggleButtonsCollection : UIViewCollection<IToggleButtonViewModel, UIToggleButtonView, UIToggleButtonLayout>
	{
		private ScrollRect _scrollRect;
		private float _defaultMoveDuration = 0.3f;

		private Tween _scrollTween;

		private Func<IUIAnimator<UIToggleButtonLayout>> _animatorFactory;

		public UIToggleButtonsCollection(UIViewCollectionLayout<UIToggleButtonLayout> layout, Func<IUIAnimator<UIToggleButtonLayout>> animatorFactory = null) :
			this(layout, null, animatorFactory)
		{
		}

		public UIToggleButtonsCollection(UIViewCollectionLayout<UIToggleButtonLayout> layout, ScrollRect scrollRect = null, Func<IUIAnimator<UIToggleButtonLayout>> animatorFactory = null) :
			base(layout)
		{
			_scrollRect      = scrollRect;
			_animatorFactory = animatorFactory;
		}

		public UIToggleButtonsCollection(UIToggleBarLayout layout) : this(layout, null)
		{
		}

		protected override UIToggleButtonView CreateViewInstance(UIToggleButtonLayout layout) =>
			new UIToggleButtonView(layout, _animatorFactory?.Invoke());

		public void TryMoveTo(IToggleButtonViewModel item, float? durationOrNull = null)
		{
			if (_scrollRect == null)
				return;

			for (int i = 0; i < UtilizedCount; i++)
			{
				if (this[i].ViewModel == item)
				{
					TryMoveTo(i, durationOrNull);
					break;
				}
			}
		}

		public void TryMoveTo(int index, float? durationOrNull = null)
		{
			if (_scrollRect == null)
				return;

			if (UtilizedCount <= 0)
				return;

			var normalizedPos = (float) index / (UtilizedCount - 1);

			_scrollTween.Kill();
			_scrollTween = _scrollRect.MoveTo(normalizedPos, durationOrNull ?? _defaultMoveDuration);
		}
	}

	public interface IToggleBarViewModel
	{
		IEnumerable<IToggleButtonViewModel> Buttons { get; }

		/// <summary>
		/// Full set of buttons has changed.
		/// </summary>
		event Action ButtonsChanged;

		void ClickBack()
		{
		}
	}
}
