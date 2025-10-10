using AssetManagement;
using Fusumity.MVVM.UI;
using Localization;
using System;
using UnityEngine;

namespace UI
{
	public class UIStatefulButtonView : UIView<IStatefulButtonViewModel, UILabeledButtonLayout>
	{
		private UIButtonWidget<UILabeledButtonLayout, WrappedArgs> _widget;

		public UIStatefulButtonView(UILabeledButtonLayout layout) : base(layout)
		{
			AddDisposable(_widget = new UIButtonWidget<UILabeledButtonLayout, WrappedArgs>());
			_widget.SetupLayout(layout);

			Subscribe(_widget, HandleClicked);
		}

		protected override void OnUpdate(IStatefulButtonViewModel viewModel)
		{
			_widget.Show(new WrappedArgs
			{
				NesterArgs = viewModel
			});

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

		//TODO: hax, need to clean up later
		private struct WrappedArgs : IButtonArgs
		{
			public IButtonArgs NesterArgs { get; set; }

			public AssetReferenceEntry<Sprite> IconReference => NesterArgs.IconReference;
			public Sprite Icon => NesterArgs.Icon;
			public LocText LocLabel => NesterArgs.LocLabel;
			public string Label => NesterArgs.Label;
		}
	}

	public interface IStatefulButtonViewModel : IButtonArgs
	{
		//TODO: add other required events

		event Action LabelChanged;
		event Action StyleChanged;

		void Click();
	}
}
