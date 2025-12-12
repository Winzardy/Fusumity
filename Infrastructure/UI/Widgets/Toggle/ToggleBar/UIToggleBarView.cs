using Fusumity.MVVM.UI;
using Sapientia.Extensions;
using System;
using System.Collections.Generic;

namespace UI
{
	public class UIToggleBarView : UIView<IToggleBarViewModel, UIToggleBarLayout>
	{
		private UIToggleButtonsCollection _collection;

		public UIToggleBarView(UIToggleBarLayout layout, Func<IWidgetAnimator<UIToggleButtonLayout>> animatorFactory = null) : base(layout)
		{
			AddDisposable(_collection = new UIToggleButtonsCollection(layout, animatorFactory));
			Subscribe(layout.back, HandleBackClicked);
		}

		protected override void OnUpdate(IToggleBarViewModel viewModel)
		{
			_collection.Update(viewModel.Buttons);
			viewModel.ButtonsChanged += HandleButtonsChanged;
		}

		protected override void OnClear(IToggleBarViewModel viewModel)
		{
			viewModel.ButtonsChanged -= HandleButtonsChanged;
		}

		private void HandleButtonsChanged()
		{
			_collection.Update(ViewModel.Buttons);
		}

		private void HandleBackClicked()
		{
			ViewModel?.ClickBack();
		}

		private class UIToggleButtonsCollection : UIViewCollection<IToggleButtonViewModel, UIToggleButtonView, UIToggleButtonLayout>
		{
			private Func<IWidgetAnimator<UIToggleButtonLayout>> _animatorFactory;

			public UIToggleButtonsCollection(UIToggleBarLayout layout, Func<IWidgetAnimator<UIToggleButtonLayout>> animatorFactory = null) : base(layout)
			{
				_animatorFactory = animatorFactory;
			}

			protected override UIToggleButtonView CreateViewInstance(UIToggleButtonLayout layout) => new UIToggleButtonView(layout, _animatorFactory?.Invoke());
		}
	}

	public interface IToggleBarViewModel
	{
		IEnumerable<IToggleButtonViewModel> Buttons { get; }

		/// <summary>
		/// Full set of buttons has changed.
		/// </summary>
		event Action ButtonsChanged;

		void ClickBack();
	}
}
