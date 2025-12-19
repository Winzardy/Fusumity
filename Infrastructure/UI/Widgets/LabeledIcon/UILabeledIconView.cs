using AssetManagement;
using Fusumity.MVVM.UI;
using Sapientia.Extensions;
using System;
using UI;
using UnityEngine;

namespace Game.UI
{
	public class UILabeledIconView : UIView<ILabeledIconViewModel, UILabeledIconLayout>
	{
		private UISpriteAssigner _assigner;

		private Sprite _defaultIconSprite;
		private string _defaultLabelText;

		public UILabeledIconView(UILabeledIconLayout layout) : base(layout)
		{
			if (_layout.icon != null)
			{
				_defaultIconSprite = _layout.icon.sprite;
			}

			if (_layout.label != null)
			{
				_defaultLabelText = _layout.label.text;
			}

			AddDisposable(_assigner = new UISpriteAssigner());

			Subscribe(_layout.labelButton, HandleLabelClicked);
			Subscribe(_layout.iconButton, HandleIconClicked);
		}

		protected override void OnUpdate(ILabeledIconViewModel viewModel)
		{
			if (viewModel.IsEmpty)
			{
				SetActive(false);
				return;
			}

			SetActive(true);

			UpdateIcon();
			UpdateLabel();
			UpdateIconColor();
			UpdateLabelColor();

			viewModel.LabelChanged += UpdateLabel;
			viewModel.IconChanged += UpdateIcon;
			viewModel.IconColorChanged += UpdateIconColor;
			viewModel.LabelColorChanged += UpdateLabelColor;
		}

		protected override void OnClear(ILabeledIconViewModel viewModel)
		{
			viewModel.LabelChanged -= UpdateLabel;
			viewModel.IconChanged -= UpdateIcon;
			viewModel.IconColorChanged -= UpdateIconColor;
			viewModel.LabelColorChanged -= UpdateLabelColor;
		}

		protected override void OnNullViewModel()
		{
			SetActive(false);
		}

		private void UpdateLabel()
		{
			if (_layout.label == null)
				return;

			_layout.label.text = ViewModel.Label ?? _defaultLabelText;
		}

		private void UpdateIcon()
		{
			if (_layout.icon == null)
				return;

			if (ViewModel.Icon.IsEmptyOrInvalid())
			{
				_layout.icon.sprite = _defaultIconSprite;
			}
			else
			{
				_assigner.TrySetSprite(_layout.icon, ViewModel.Icon);
			}
		}

		private void UpdateIconColor()
		{
			if (_layout.icon == null)
				return;

			if (ViewModel?.IconColor == null)
				return;

			_layout.icon.color = ViewModel.IconColor.Value;
		}

		private void UpdateLabelColor()
		{
			if (_layout.label == null)
				return;

			if (ViewModel?.LabelColor == null)
				return;

			_layout.label.color = ViewModel.LabelColor.Value;
		}

		private void HandleLabelClicked() => ViewModel?.LabelClick();
		private void HandleIconClicked() => ViewModel?.IconClick();
	}

	public interface ILabeledIconViewModel
	{
		public string Label { get; }
		public UISpriteInfo Icon { get; }
		public Color? IconColor { get => null; }
		public Color? LabelColor { get => null; }
		public bool IsEmpty { get => Label.IsNullOrEmpty() && Icon.IsEmptyOrInvalid(); }

		public event Action LabelChanged;
		public event Action IconChanged;
		public event Action IconColorChanged;
		public event Action LabelColorChanged;

		public void LabelClick()
		{
		}

		public void IconClick()
		{
		}
	}
}
