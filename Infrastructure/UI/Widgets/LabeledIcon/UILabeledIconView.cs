using System;
using System.Collections.Generic;
using Fusumity.MVVM.UI;
using Sapientia.Extensions;
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

		public UILabeledIconView(UILabeledIconLayout layout, bool disableIfEmpty = false) : base(layout)
		{
			_disableIfEmpty = disableIfEmpty;

			if (_layout.icon != null)
			{
				_defaultIconSprite = _layout.icon.sprite;
				_defaultIconColor  = _layout.icon.color;
			}

			_defaultLabelText = _layout.label.text;

			AddDisposable(_spriteAssigner = new UISpriteAssigner());

			Subscribe(_layout.labelButton, HandleLabelClicked);
			Subscribe(_layout.iconButton, HandleIconClicked);
		}

		protected override void OnUpdate(ILabeledIconViewModel viewModel)
		{
			SetActive(!_disableIfEmpty || !viewModel.IsEmpty);

			UpdateLabel();
			UpdateIcon();

			UpdateIconColor();
			UpdateLabelColor();

			UpdateStyle();

			viewModel.LabelChanged      += HandleLabelChanged;
			viewModel.LabelColorChanged += UpdateLabelColor;
			viewModel.StyleChanged      += HandleLabelStyleChanged;

			viewModel.IconChanged      += UpdateIcon;
			viewModel.IconColorChanged += UpdateIconColor;
		}

		protected override void OnClear(ILabeledIconViewModel viewModel)
		{
			viewModel.LabelChanged      -= HandleLabelChanged;
			viewModel.LabelColorChanged -= UpdateLabelColor;
			viewModel.StyleChanged      -= HandleLabelStyleChanged;

			viewModel.IconChanged      -= UpdateIcon;
			viewModel.IconColorChanged -= UpdateIconColor;
		}

		protected override void OnNullViewModel()
		{
			SetActive(false);
		}

		private void HandleLabelChanged() => UpdateLabel();

		private void UpdateLabel()
		{
			if (_disableIfEmpty)
			{
				SetActive(!ViewModel.IsEmpty);
			}

			_layout.label.text = ViewModel.Label ?? _defaultLabelText;
		}

		private void HandleLabelStyleChanged()
		{
			UpdateStyle();
		}

		private void UpdateStyle()
		{
			if (_layout.labelStyleSwitcher)
				_layout.labelStyleSwitcher.Switch(ViewModel.Style);
		}

		private void UpdateIcon()
		{
			if (_layout.icon == null)
				return;

			if (_disableIfEmpty)
			{
				SetActive(!ViewModel.IsEmpty);
			}

			_spriteAssigner.TryCancelOrClear(_layout.icon);

			if (ViewModel.Icon.IsEmptyOrInvalid())
			{
				_layout.icon.sprite = _defaultIconSprite;
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
	}

	public interface ILabeledIconViewModel
	{
		string Label { get; }
		Color? LabelColor { get => null; }
		string Style { get => null; }

		UISpriteInfo Icon { get; }
		Color? IconColor { get => null; }

		bool IsEmpty { get => Label.IsNullOrEmpty() && Icon.IsEmptyOrInvalid(); }

		event Action LabelChanged;
		event Action LabelColorChanged;
		event Action StyleChanged;

		event Action IconChanged;
		event Action IconColorChanged;

		void LabelClick()
		{
		}

		void IconClick()
		{
		}
	}

	public sealed class LabeledIconComparer : IEqualityComparer<ILabeledIconViewModel>
	{
		public static LabeledIconComparer Instance { get; } = new();

		public static bool Equals(ILabeledIconViewModel a, ILabeledIconViewModel b)
		{
			return Instance.EqualsInternal(a, b);
		}

		private bool EqualsInternal(ILabeledIconViewModel a, ILabeledIconViewModel b)
		{
			if (a == null || b == null)
				return a == b;

			return a.Label == b.Label &&
				a.Style == b.Style &&
				a.LabelColor == b.LabelColor &&
				a.Icon.Equals(b.Icon) &&
				a.IconColor == b.IconColor;
		}

		bool IEqualityComparer<ILabeledIconViewModel>.Equals(ILabeledIconViewModel a, ILabeledIconViewModel b)
			=> EqualsInternal(a, b);

		int IEqualityComparer<ILabeledIconViewModel>.GetHashCode(ILabeledIconViewModel obj) => obj.GetHashCode();
	}
}
