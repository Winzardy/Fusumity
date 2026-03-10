using Fusumity.MVVM.UI;
using Fusumity.Utility;
using Game.UI;
using Sapientia.Collections;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace UI
{
	public class UIPricedButtonView : UIView<IPricedButtonViewModel, UIPricedButtonLayout>
	{
		private UIStatefulButtonView _button;
		private UILabeledIconCollection _price;
		private UILabeledIconView _primaryPrice;

		public UIPricedButtonView(UIPricedButtonLayout layout) : base(layout)
		{
			AddDisposable(_button = new UIStatefulButtonView(layout));
			AddDisposable(_price  = new UILabeledIconCollection(layout.prices));
			if (layout.primaryPrice)
				AddDisposable(_primaryPrice = new UILabeledIconView(layout.primaryPrice));
		}

		protected override void OnUpdate(IPricedButtonViewModel viewModel)
		{
			_button.Update(viewModel);
			UpdatePrices();

			viewModel.PricesChanged += UpdatePrices;
		}

		protected override void OnClear(IPricedButtonViewModel viewModel)
		{
			viewModel.PricesChanged -= UpdatePrices;

			_button.ClearViewModel();
			_price.Reset();
			_primaryPrice?.ClearViewModel();
		}

		private void UpdatePrices()
		{
			_price.UpdateOrDeactivate(ViewModel.Prices);

			if (ViewModel.PrimaryPrice != null)
			{
				_primaryPrice?.Update(ViewModel.PrimaryPrice);
			}
			else
			{
				_primaryPrice?.ClearViewModel();
				_primaryPrice?.SetActive(false);
			}
		}
	}

	public interface IPricedButtonViewModel : IStatefulButtonViewModel
	{
		IEnumerable<ILabeledIconViewModel> Prices { get; }
		[CanBeNull] ILabeledIconViewModel PrimaryPrice { get => null; }

		event Action PricesChanged;
	}
}
