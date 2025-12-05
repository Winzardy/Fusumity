using AssetManagement;
using Fusumity.MVVM.UI;
using Localization;
using System;
using UI;
using UnityEngine;

namespace Game.UI
{
	public class UILabeledIconView : UIView<ILabeledIconViewModel, UILabeledIconLayout>
	{
		private UISpriteAssigner _spriteAssigner;
		private UITextLocalizationAssigner _locTextAssigner;

		private Sprite _defaultIconSprite;
		private string _defaultLabelText;

		public UILabeledIconView(UILabeledIconLayout layout) : base(layout)
		{
			if (_layout.icon)
				_defaultIconSprite = _layout.icon.sprite;
			if (_layout.label)
				_defaultLabelText = _layout.label.text;

			AddDisposable(_spriteAssigner = new UISpriteAssigner());
			AddDisposable(_locTextAssigner = new UITextLocalizationAssigner());

			Subscribe(_layout.labelButton, HandleLabelClicked);
			Subscribe(_layout.iconButton, HandleIconClicked);
		}

		protected override void OnUpdate(ILabeledIconViewModel viewModel)
		{
			UpdateIcon();
			UpdateLabel();

			viewModel.LabelChanged += UpdateLabel;
			viewModel.IconChanged += UpdateIcon;
		}

		protected override void OnClear(ILabeledIconViewModel viewModel)
		{
			viewModel.LabelChanged -= UpdateLabel;
			viewModel.IconChanged -= UpdateIcon;
		}

		private void UpdateLabel()
		{
			_layout.label.SetTextSafe
			(
				_locTextAssigner,
				ViewModel.LocLabel,
				ViewModel.Label,
				_defaultLabelText
			);
		}

		private void UpdateIcon()
		{
			_layout.icon.SetSpriteSafe
			(
				_spriteAssigner,
				ViewModel.IconRef,
				ViewModel.Icon,
				_defaultIconSprite
			);
		}

		private void HandleLabelClicked() => ViewModel?.LabelClick();
		private void HandleIconClicked() => ViewModel?.IconClick();
	}

	public interface ILabeledIconViewModel
	{
		public string Label { get; }
		public LocText LocLabel { get => default; }
		public Sprite Icon { get => null; }
		public AssetReferenceEntry<Sprite> IconRef { get => null; }

		public event Action LabelChanged;
		public event Action IconChanged;

		public void LabelClick()
		{
		}

		public void IconClick()
		{
		}
	}
}
