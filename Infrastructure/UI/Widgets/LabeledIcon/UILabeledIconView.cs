using Fusumity.MVVM.UI;
using Sapientia.Extensions;
using System;
using JetBrains.Annotations;
using UI;
using UnityEngine;

namespace Game.UI
{
	public class UILabeledIconView : UIView<ILabeledIconViewModel, UILabeledIconLayout>
	{
		private UISpriteAssigner _spriteAssigner;
		private bool _disableIfEmpty;

		private Sprite _defaultIconSprite;
		private Color _defaultIconColor;
		private string _defaultLabelText;
		private string _defaultSubLabelText;

		public UILabeledIconView(UILabeledIconLayout layout, bool disableIfEmpty = false) : base(layout)
		{
			_disableIfEmpty = disableIfEmpty;

			if (_layout.icon != null)
			{
				_defaultIconSprite = _layout.icon.sprite;
				_defaultIconColor = _layout.icon.color;
			}

			_defaultLabelText = _layout.label.text;
			Subscribe(_layout.labelButton, HandleLabelClicked);

			if (_layout.subLabel != null)
			{
				_defaultLabelText = _layout.label.text;
				Subscribe(_layout.subLabelButton, HandleSubLabelClicked);
			}

			AddDisposable(_spriteAssigner = new UISpriteAssigner());

			Subscribe(_layout.iconButton, HandleIconClicked);
		}

		protected override void OnUpdate(ILabeledIconViewModel viewModel)
		{
			SetActive(!_disableIfEmpty || !viewModel.IsEmpty);

			UpdateIcon();

			UpdateIconColor();
			UpdateLabelColor();

			if (!viewModel.Label.IsNullOrEmpty())
				_layout.label.Bind(viewModel.Label, UpdateLabel);

			_layout.subLabel.BindSafe(viewModel.SubLabel, UpdateLabel);

			viewModel.IconChanged += UpdateIcon;
			viewModel.IconColorChanged += UpdateIconColor;
			viewModel.LabelColorChanged += UpdateLabelColor;

			viewModel.LabelStyleChanged += HandleLabelStyleChanged;
			viewModel.SubLabelStyleChanged += HandleSubLabelStyleChanged;
		}

		protected override void OnClear(ILabeledIconViewModel viewModel)
		{
			if (!viewModel.Label.IsNullOrEmpty())
				_layout.label.Unbind(viewModel.Label);

			_layout.subLabel.UnbindSafe(viewModel.Label);

			viewModel.IconChanged -= UpdateIcon;
			viewModel.IconColorChanged -= UpdateIconColor;
			viewModel.LabelColorChanged -= UpdateLabelColor;

			viewModel.LabelStyleChanged -= HandleLabelStyleChanged;
			viewModel.SubLabelStyleChanged -= HandleSubLabelStyleChanged;
		}

		protected override void OnNullViewModel()
		{
			SetActive(false);
		}

		private void UpdateLabel(string text)
		{
			if (_disableIfEmpty)
			{
				SetActive(!ViewModel.IsEmpty);
			}

			_layout.label.text = text ?? _defaultLabelText;
		}

		private void UpdateSubLabel(string text)
		{
			if (_layout.label == null)
				return;

			if (_disableIfEmpty)
			{
				SetActive(!ViewModel.IsEmpty);
			}

			_layout.subLabel.text = text ?? _defaultSubLabelText;
		}

		private void HandleLabelStyleChanged()
		{
			if (_layout.labelStyleSwitcher)
				_layout.labelStyleSwitcher.Switch(ViewModel.LabelStyle);
		}

		private void HandleSubLabelStyleChanged()
		{
			if (_layout.subLabelStyleSwitcher)
				_layout.subLabelStyleSwitcher.Switch(ViewModel.SubLabelStyle);
		}

		private void UpdateIcon()
		{
			if (_layout.icon == null)
				return;

			if (_disableIfEmpty)
			{
				SetActive(!ViewModel.IsEmpty);
			}

			if (ViewModel.Icon.IsEmptyOrInvalid())
			{
				_layout.icon.sprite = _defaultIconSprite;
				_layout.icon.color = _defaultIconColor;
			}
			else
			{
				_spriteAssigner.TrySetSprite(_layout.icon, ViewModel.Icon);
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

		private void HandleIconClicked() => ViewModel?.IconClick();

		private void HandleLabelClicked() => ViewModel?.LabelClick();
		private void HandleSubLabelClicked() => ViewModel?.SubLabelClick();
	}

	public interface ILabeledIconViewModel
	{
		[CanBeNull] ILabelViewModel Label { get; }
		string LabelStyle { get => null; }

		[CanBeNull] ILabelViewModel SubLabel { get => null; }
		string SubLabelStyle { get => null; }
		UISpriteInfo Icon { get; }
		Color? IconColor { get => null; }
		Color? LabelColor { get => null; }
		bool IsEmpty { get => SubLabel.IsNullOrEmpty() && Icon.IsEmptyOrInvalid() && SubLabel.IsNullOrEmpty(); }

		event Action IconChanged;
		event Action IconColorChanged;
		public event Action LabelColorChanged;
		event Action LabelStyleChanged;
		event Action SubLabelStyleChanged;

		public void LabelClick()
		{
		}

		public void SubLabelClick()
		{
		}

		public void IconClick()
		{
		}
	}
}
