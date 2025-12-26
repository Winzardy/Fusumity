using Fusumity.MVVM.UI;
using Fusumity.Utility;
using Game.UI;
using Sapientia.Collections;
using System;
using System.Collections.Generic;

namespace UI
{
	public class UIPricedButtonView : UIView<IPricedButtonViewModel, UIPricedButtonLayout>
	{
		private UIStatefulButtonView _button;
		private UILabeledIconCollection _price;

		public UIPricedButtonView(UIPricedButtonLayout layout) : base(layout)
		{
			AddDisposable(_button = new UIStatefulButtonView(layout));
			AddDisposable(_price = new UILabeledIconCollection(layout.prices));
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
		}

		private void UpdatePrices()
		{
			if (ViewModel.Prices.IsNullOrEmpty())
			{
				_layout.prices.root.SetActive(false);
			}
			else
			{
				_layout.prices.root.SetActive(true);
				_price.Update(ViewModel.Prices);
			}
		}
	}

	public interface IPricedButtonViewModel : IStatefulButtonViewModel
	{
		IEnumerable<ILabeledIconViewModel> Prices { get; }

		event Action PricesChanged;
	}
}
