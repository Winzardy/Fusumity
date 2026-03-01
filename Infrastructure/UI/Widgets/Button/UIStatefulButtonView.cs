using System;
using Fusumity.MVVM.UI;
using Fusumity.Utility;
using Game.UI;
using Sapientia.Extensions;

namespace UI
{
	public class UIStatefulButtonView : UIView<IStatefulButtonViewModel, UIStatefulButtonLayout>, IClickable<UIStatefulButtonView>
	{
		private UIAdBannerView _adBanner;
		private UILabeledIconView _labeledIcon;
		private UISpriteAssigner _assigner;

		public event Action<UIStatefulButtonView> Clicked;

		public UIStatefulButtonView(UIStatefulButtonLayout layout) : base(layout)
		{
			if (layout.adBanner != null)
			{
				AddDisposable(_adBanner = new UIAdBannerView(layout.adBanner));
			}

			if (layout.labeledIcon != null)
			{
				AddDisposable(_labeledIcon = new UILabeledIconView(layout.labeledIcon, true));
			}

			Subscribe(layout, HandleClick);
		}

		protected override void OnUpdate(IStatefulButtonViewModel viewModel)
		{
			SetActive(true);

			if (_layout.label != null)
				viewModel.Label.Bind(UpdateLabel);

			_adBanner?.Update(viewModel.AdBanner);
			_labeledIcon?.Update(viewModel.LabeledIcon);

			UpdateIcon();
			UpdateStyle();
			UpdateInteractable();

			viewModel.StyleChanged += UpdateStyle;
			viewModel.InteractableChanged += UpdateInteractable;
		}

		protected override void OnClear(IStatefulButtonViewModel viewModel)
		{
			if (_layout.label != null)
				viewModel.Label.Release();

			viewModel.StyleChanged -= UpdateStyle;
			viewModel.InteractableChanged -= UpdateInteractable;
		}

		protected override void OnNullViewModel()
		{
			SetActive(false);
		}

		private void UpdateLabel(string text)
		{
			_layout.label.text = text;
			_layout.label.SetActive(!text.IsNullOrEmpty());
		}

		private void UpdateStyle()
		{
			if (_layout.switcher != null)
				_layout.switcher.Switch(ViewModel.Style);
		}

		private void UpdateInteractable()
		{
			_layout.button.interactable = !ViewModel.Interactable.HasValue || ViewModel.Interactable.Value;
		}

		private void UpdateIcon()
		{
			if (_layout.icon == null)
				return;

			if (ViewModel.Icon.IsEmptyOrInvalid())
				return;

			if (_assigner == null)
			{
				AddDisposable(_assigner = new UISpriteAssigner());
			}

			_assigner.TrySetSprite(_layout.icon, ViewModel.Icon);
		}

		private void HandleClick()
		{
			Clicked?.Invoke(this);
			ViewModel?.Click();
		}
	}

	public interface IStatefulButtonViewModel
	{
		ILabelViewModel Label { get; }
		string Style { get; }

		bool? Interactable { get => null; }
		UISpriteInfo Icon { get => default; }
		IAdBannerViewModel AdBanner { get => null; }
		ILabeledIconViewModel LabeledIcon { get => null; }

		event Action StyleChanged;
		event Action InteractableChanged;

		void Click();
	}
}
