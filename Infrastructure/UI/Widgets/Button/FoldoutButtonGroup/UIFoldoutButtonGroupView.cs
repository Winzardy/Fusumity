using System;
using System.Collections.Generic;
using Fusumity.MVVM.UI;

namespace UI.FoldoutButtonGroup
{
	public class UIFoldoutButtonGroupView : UIView<IFoldoutButtonGroupViewModel, UIFoldoutButtonGroupLayout>
	{
		private UIToggleButtonView _toggle;
		private UIStatefulButtonCollection _items;

		private UIAnimator<UIFoldoutButtonGroupLayout> _animator;

		public UIFoldoutButtonGroupView(UIFoldoutButtonGroupLayout layout) : base(layout)
		{
			AddDisposable(_items = new UIStatefulButtonCollection(layout.items));
			AddDisposable(_toggle = new UIToggleButtonView(layout.toggle));
			AddDisposable(_animator = new UIAnimator<UIFoldoutButtonGroupLayout>(layout));
		}

		protected override void OnUpdate(IFoldoutButtonGroupViewModel viewModel)
		{
			SetActive(true);

			UpdateItems();
			viewModel.ItemsUpdated += HandleItemsUpdated;

			_toggle.Update(viewModel.Toggle);

			UpdateGroup(true);
			viewModel.Toggle.ToggleStateChanged += HandleToggleStateChanged;
		}

		protected override void OnClear(IFoldoutButtonGroupViewModel viewModel)
		{
			viewModel.ItemsUpdated -= HandleItemsUpdated;

			viewModel.Toggle.ToggleStateChanged -= HandleToggleStateChanged;
		}

		protected override void OnNullViewModel()
		{
			SetActive(false);
		}

		private void HandleToggleStateChanged(bool immediate)
		{
			UpdateGroup(immediate);
		}

		private void HandleItemsUpdated()
		{
			UpdateItems();
		}

		private void UpdateItems()
		{
			_items.Update(ViewModel.Items);
		}

		private void UpdateGroup(bool immediate)
		{
			_animator.Play(ViewModel.Toggle.IsToggled
					? AnimationType.OPENING
					: AnimationType.CLOSING,
				immediate);
		}
	}

	public interface IFoldoutButtonGroupViewModel
	{
		IToggleButtonViewModel Toggle { get; }

		IEnumerable<IStatefulButtonViewModel> Items { get; }

		event Action ItemsUpdated;
	}
}
