using AssetManagement;
using Fusumity.MVVM.UI;
using Localization;
using System;
using UnityEngine;

namespace UI
{
	public class UIStatefulButtonView : UIView<IStatefulButtonViewModel, UILabeledButtonLayout>
	{
		private UIButtonWidget<UILabeledButtonLayout, IStatefulButtonViewModel> _widget;

		public UIStatefulButtonView(UILabeledButtonLayout layout) : base(layout)
		{
			AddDisposable(_widget = new UIButtonWidget<UILabeledButtonLayout, IStatefulButtonViewModel>(layout));

			Subscribe(_widget, HandleClicked);
		}

		protected override void OnUpdate(IStatefulButtonViewModel viewModel)
		{
			_widget.Show(viewModel);

			viewModel.LabelChanged += HandleLabelChanged;
			viewModel.StyleChanged += HandleStyleChanged;
		}

		protected override void OnClear(IStatefulButtonViewModel viewModel)
		{
			viewModel.LabelChanged -= HandleLabelChanged;
			viewModel.StyleChanged -= HandleStyleChanged;
		}

		private void HandleLabelChanged()
		{
			_widget.SetLabel(ViewModel.LocLabel, ViewModel.Label);
		}

		private void HandleStyleChanged()
		{
			_widget.SetStyle(ViewModel.Style);
		}

		private void HandleClicked(IButtonWidget widget)
		{
			ViewModel?.Click();
		}
	}

	public interface IStatefulButtonViewModel : IObseleteButtonViewModel
	{
		//TODO: add other required events

		event Action LabelChanged;
		event Action StyleChanged;

		void Click();
	}
}
