using Fusumity.MVVM.UI;
using Game.UI;
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
			//AddDisposable(_button = new UIStatefulButtonView(layout));
			AddDisposable(_price = new UILabeledIconCollection(layout.prices));
		}

		protected override void OnUpdate(IPricedButtonViewModel viewModel)
		{
			_button.Update(viewModel);
			_price.Update(ViewModel.Price);

			viewModel.PriceChanged += HandlePriceChanged;
		}

		protected override void OnClear(IPricedButtonViewModel viewModel)
		{
			viewModel.PriceChanged -= HandlePriceChanged;
		}

		private void HandlePriceChanged()
		{
			_price.Update(ViewModel.Price);
		}
	}

	public interface IPricedButtonViewModel : IStatefulButtonViewModel
	{
		IEnumerable<ILabeledIconViewModel> Price { get; }

		event Action PriceChanged;
	}
}
