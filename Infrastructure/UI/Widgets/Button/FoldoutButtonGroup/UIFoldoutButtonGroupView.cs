using System;
using System.Collections.Generic;
using Fusumity.MVVM.UI;
using InputManagement;

namespace UI
{
	public class UIFoldoutButtonGroupView : UIView<IFoldoutButtonGroupViewModel, UIFoldoutButtonGroupLayout>
	{
		private UIToggleButtonView _toggle;
		private UIStatefulButtonCollection _items;

		private UIRectTapDetector _tapDetector;

		public UIFoldoutButtonGroupView(UIFoldoutButtonGroupLayout layout) : base(layout)
		{
			AddDisposable(_items  = new UIStatefulButtonCollection(layout.items));
			AddDisposable(_toggle = new UIToggleButtonView(layout.toggle));
			AddDisposable(_tapDetector = new UIRectTapDetector
				(
					layout.rectTransform,
					HandleOutOfBoundsClicked,
					rects: _layout.items.rectTransform
				)
			);
		}

		protected override void OnUpdate(IFoldoutButtonGroupViewModel viewModel)
		{
			SetActive(true);

			UpdateItems();
			viewModel.ItemsUpdated += HandleItemsUpdated;

			_toggle.Update(viewModel.Toggle);

			_tapDetector.SetInputReader(viewModel.InputReader);
			_tapDetector.SetActive(true);
		}

		protected override void OnClear(IFoldoutButtonGroupViewModel viewModel)
		{
			viewModel.ItemsUpdated -= HandleItemsUpdated;

			_tapDetector.SetActive(false);
		}

		protected override void OnNullViewModel()
		{
			SetActive(false);
		}

		private void HandleItemsUpdated()
		{
			UpdateItems();
		}

		private void UpdateItems()
		{
			_items.Update(ViewModel.Items);
		}

		private void HandleOutOfBoundsClicked()
		{
			ViewModel?.ClickOutOfBounds();
		}
	}

	public interface IFoldoutButtonGroupViewModel
	{
		IInputReader InputReader { get; }

		IToggleButtonViewModel Toggle { get; }

		IEnumerable<IStatefulButtonViewModel> Items { get; }

		event Action ItemsUpdated;

		void ClickOutOfBounds();
	}
}
