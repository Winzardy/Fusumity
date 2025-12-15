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

			UpdateLabel();
			UpdateStyle();
			UpdateInteractable();

			viewModel.LabelChanged += UpdateLabel;
			viewModel.StyleChanged += UpdateStyle;
			viewModel.InteractableChanged += UpdateInteractable;
		}

		protected override void OnClear(IStatefulButtonViewModel viewModel)
		{
			viewModel.LabelChanged -= UpdateLabel;
			viewModel.StyleChanged -= UpdateStyle;
			viewModel.InteractableChanged -= UpdateInteractable;
		}

		private void UpdateLabel()
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

		private void UpdateStyle()
		{
			_widget.SetStyle(ViewModel.Style);
		}

		private void UpdateInteractable()
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
