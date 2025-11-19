using System;
using AssetManagement;
using Localization;
using Sapientia.Extensions;
using UI;
using UnityEngine;

namespace Game.UI
{
	public class UILabeledIcon : UIWidget<UILabeledIconLayout, ILabeledIconViewModel>
	{
		private UISpriteAssigner _spriteAssigner;
		private UITextLocalizationAssigner _locTextAssigner;

		private Sprite _defaultIconSprite;
		private string _defaultLabelText;

		protected override void OnLayoutInstalled()
		{
			Create(out _spriteAssigner);
			Create(out _locTextAssigner);

			_layout.labelButton.Subscribe(OnLabelClicked);
			_layout.iconButton.Subscribe(OnIconClicked);

			if (_layout.icon)
				_defaultIconSprite = _layout.icon.sprite;
			if (_layout.label)
				_defaultLabelText = _layout.label.text;
		}

		protected override void OnLayoutCleared()
		{
			_layout.labelButton.Unsubscribe(OnLabelClicked);
			_layout.iconButton.Unsubscribe(OnIconClicked);
		}

		protected override void OnShow(ref ILabeledIconViewModel vm)
		{
			UpdateIcon();
			vm.IconChanged += OnIconChanged;

			UpdateLabel();
			vm.LabelChanged += OnLabelChanged;
		}

		protected override void OnHide(ref ILabeledIconViewModel vm)
		{
			vm.IconChanged -= OnIconChanged;
			vm.LabelChanged -= OnLabelChanged;
		}

		private void OnLabelChanged() => UpdateLabel();

		private void UpdateLabel()
		{
			_layout.label.SetTextSafe
			(
				_locTextAssigner,
				vm.LocLabel,
				vm.Label,
				_defaultLabelText
			);
		}

		private void OnIconChanged() => UpdateIcon();

		private void UpdateIcon()
		{
			_layout.icon.SetSpriteSafe
			(
				_spriteAssigner,
				vm.IconRef,
				vm.Icon,
				_defaultIconSprite
			);
		}

		private void OnLabelClicked() => vm.LabelClick();

		private void OnIconClicked() => vm.IconClick();
	}

	public interface ILabeledIconViewModel
	{
		public AssetReferenceEntry<Sprite> IconRef => null;
		public Sprite Icon => null;
		public event Action IconChanged;

		public string Label { get; }
		public LocText LocLabel => default;
		public event Action LabelChanged;

		public void LabelClick()
		{
		}

		public void IconClick()
		{
		}
	}
}
