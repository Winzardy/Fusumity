using Fusumity.MVVM.UI;
using System;
using System.Collections.Generic;
using ActionBusSystem;
using Sapientia.Extensions;

namespace UI
{
	public class UIToggleBarView : UIView<IToggleBarViewModel, UIToggleBarLayout>
	{
		private UIToggleButtonsCollection _collection;

		private ActionBusElement _backClickElement;

		public UIToggleBarView(UIToggleBarLayout layout, Func<IUIAnimator<UIToggleButtonLayout>> animatorFactory = null) : base(layout)
		{
			AddDisposable(_collection = new UIToggleButtonsCollection(layout, animatorFactory));

			if (layout.back != null)
				_backClickElement = Subscribe(layout.back, HandleBackClicked, _layout.backButtonUniqueId, _layout.backButtonGroupId);
		}

		protected override void OnUpdate(IToggleBarViewModel viewModel)
		{
			SetActive(true);

			_collection.Update(viewModel.Buttons);
			viewModel.ButtonsChanged += HandleButtonsChanged;

			if (!viewModel.BackButtonActionBusUniqueId.IsNullOrEmpty() || !viewModel.BackButtonActionBusGroupId.IsNullOrEmpty())
				UpdateBackClickElement(viewModel.BackButtonActionBusUniqueId, viewModel.BackButtonActionBusGroupId);
		}

		protected override void OnClear(IToggleBarViewModel viewModel)
		{
			viewModel.ButtonsChanged -= HandleButtonsChanged;
			UpdateBackClickElement();
		}

		private void UpdateBackClickElement(string uid = null, string groupId = null)
		{
			if (_layout.back == null)
				return;

			DisposeAndRemoveDisposable(_backClickElement);
			_backClickElement = Subscribe(_layout.back, HandleBackClicked,
				uid ?? _layout.backButtonUniqueId,
				groupId ?? _layout.backButtonGroupId);
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
		private Func<IUIAnimator<UIToggleButtonLayout>> _animatorFactory;

		public UIToggleButtonsCollection(UIViewCollectionLayout<UIToggleButtonLayout> layout, Func<IUIAnimator<UIToggleButtonLayout>> animatorFactory = null) :
			base(layout)
		{
			_animatorFactory = animatorFactory;
		}

		protected override UIToggleButtonView CreateViewInstance(UIToggleButtonLayout layout) =>
			new UIToggleButtonView(layout, _animatorFactory?.Invoke());
	}

	public interface IToggleBarViewModel
	{
		IEnumerable<IToggleButtonViewModel> Buttons { get; }

		/// <summary>
		/// Full set of buttons has changed.
		/// </summary>
		event Action ButtonsChanged;

		string BackButtonActionBusUniqueId { get => null; }
		string BackButtonActionBusGroupId { get => null; }

		void ClickBack()
		{
		}
	}
}
