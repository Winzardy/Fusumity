using AssetManagement;
using Fusumity.MVVM.UI;
using System;

namespace UI
{
	public class UIToggleButtonView : UIView<IToggleButtonViewModel, UIToggleButtonLayout>
	{
		private UISpriteAssigner _iconAssigner;
		private IWidgetAnimator<UIToggleButtonLayout> _animator;

		public UIToggleButtonView(UIToggleButtonLayout layout) : base(layout)
		{
			AddDisposable(_iconAssigner = new UISpriteAssigner());
			AddDisposable(_animator = new DefaultToggleWidgetAnimator());
			Subscribe(layout, HandleClicked);

			_animator.SetupLayout(layout);
		}

		protected override void OnUpdate(IToggleButtonViewModel viewModel)
		{
			UpdateIcon(viewModel.Icon);
			UpdateLabel(viewModel.Label);
			UpdateToggleState(viewModel.IsToggled, true);

			_layout.styleSwitcher?.Switch(viewModel.Style);

			viewModel.ToggleStateChanged += HandleToggleStateChanged;
			viewModel.StyleChanged += HandleAvailableStateChanged;
			viewModel.IconChanged += HandleIconChanged;
			viewModel.LabelChanged += HandleLabelChanged;
		}

		protected override void OnClear(IToggleButtonViewModel viewModel)
		{
			viewModel.ToggleStateChanged -= HandleToggleStateChanged;
			viewModel.StyleChanged -= HandleAvailableStateChanged;
			viewModel.IconChanged -= HandleIconChanged;
			viewModel.LabelChanged -= HandleLabelChanged;
		}

		private void UpdateIcon(IAssetReferenceEntry icon)
		{
			if (!icon.IsEmpty())
			{
				_iconAssigner.SetSprite(_layout.icon, icon);
			}
		}

		private void UpdateToggleState(bool isToggled, bool immediate)
		{
			var key =
				isToggled ?
				WidgetAnimationType.TOGGLE_ENABLING :
				WidgetAnimationType.TOGGLE_DISABLING;

			_animator.Play(key, immediate);
		}

		private void UpdateLabel(string label)
		{
			if (_layout.label != null)
			{
				_layout.label.text = label;
			}
		}

		private void HandleAvailableStateChanged()
		{
			_layout.styleSwitcher?.Switch(ViewModel.Style);
		}

		private void HandleToggleStateChanged()
		{
			UpdateToggleState(ViewModel.IsToggled, false);
		}

		private void HandleIconChanged()
		{
			UpdateIcon(ViewModel.Icon);
		}

		private void HandleLabelChanged()
		{
			UpdateLabel(ViewModel.Label);
		}

		private void HandleClicked()
		{
			ViewModel?.Click();
		}
	}

	public interface IToggleButtonViewModel
	{
		IAssetReferenceEntry Icon { get; }
		string Label { get; }

		bool IsToggled { get; }
		string Style { get; }

		event Action ToggleStateChanged;
		event Action StyleChanged;
		event Action IconChanged;
		event Action LabelChanged;

		void Click();
	}

	public static class ToggleButtonStyle
	{
		public const string LOCKED = "Locked";
		public const string UNLOCKED = "Unlocked";
	}
}
