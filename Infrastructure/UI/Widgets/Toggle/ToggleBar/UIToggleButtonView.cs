using AssetManagement;
using Fusumity.MVVM.UI;
using System;
using JetBrains.Annotations;
using UnityEngine;

namespace UI
{
	public class UIToggleButtonView : UIView<IToggleButtonViewModel, UIToggleButtonLayout>
	{
		private UISpriteAssigner _iconAssigner;
		private IWidgetAnimator<UIToggleButtonLayout> _animator;

		public UIToggleButtonView(UIToggleButtonLayout layout, IWidgetAnimator<UIToggleButtonLayout> animator = null) : base(layout)
		{
			AddDisposable(_iconAssigner = new UISpriteAssigner());
			Subscribe(layout, HandleClicked);

			if (animator != null)
			{
				_animator = animator;
				_animator.SetupLayout(layout);

				AddDisposable(animator);
			}
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

		private void UpdateIcon(IAssetReferenceEntry<Sprite> icon)
		{
			_iconAssigner.TrySetSprite(_layout.icon, icon);
		}

		private void UpdateToggleState(bool isToggled, bool immediate)
		{
			if (_animator != null)
			{
				var key =
					isToggled ? WidgetAnimationType.TOGGLE_ENABLING : WidgetAnimationType.TOGGLE_DISABLING;

				_animator.Play(key, immediate);
			}

			_layout.activitySwitcher?.Switch(isToggled);
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
		[CanBeNull] IAssetReferenceEntry<Sprite> Icon { get; }
		[CanBeNull] string Label { get; }

		bool IsToggled { get; }
		[CanBeNull] string Style { get; }

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
