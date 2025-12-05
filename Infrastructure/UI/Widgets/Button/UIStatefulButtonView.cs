using Fusumity.MVVM.UI;
using Sapientia.Extensions;
using System;

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
			viewModel.InteractableChanged += HandleInteractableChanged;
		}

		protected override void OnClear(IStatefulButtonViewModel viewModel)
		{
			viewModel.LabelChanged -= HandleLabelChanged;
			viewModel.StyleChanged -= HandleStyleChanged;
			viewModel.InteractableChanged -= HandleInteractableChanged;
		}

		private void HandleLabelChanged()
		{
			if (ViewModel.Label.IsNullOrEmpty())
			{
				_widget.SetLabel(ViewModel.LocLabel, ViewModel.Label);
			}
			else
			{
				_layout.label.text = ViewModel.Label;
			}
		}

		private void HandleStyleChanged()
		{
			_widget.SetStyle(ViewModel.Style);
		}

		private void HandleInteractableChanged()
		{
			_widget.SetInteractable(!ViewModel.Interactable.HasValue || ViewModel.Interactable.Value);
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
		event Action InteractableChanged;

		void Click();
	}
}
