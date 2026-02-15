using System;
using AssetManagement;
using Fusumity.Utility;
using Localization;
using Sapientia.Extensions;
using UnityEngine;

namespace UI
{
	public class DefaultButtonViewModel : IButtonViewModel
	{
		public AssetReferenceEntry<Sprite> IconReference { get; private set; }
		public Sprite Icon { get; set; }
		public event Action IconChanged;

		public LocText LocLabel { get; set; }
		public string Label { get; set; }
		public event Action LabelChanged;

		public bool? Interactable { get; set; }
		public event Action<bool> InteractableChanged;

		public string Style { get; set; }
		public event Action<string> StyleChanged;

		public Action Action { get; set; }

		public void SetStyle(string style)
		{
			Style = style;
			StyleChanged?.Invoke(style);
		}

		public void SetInteractable(bool value)
		{
			Interactable = value;
			InteractableChanged?.Invoke(value);
		}
	}

	public interface IButtonViewModel
	{
		public AssetReferenceEntry<Sprite> IconReference { get; }
		public Sprite Icon { get; }
		public event Action IconChanged;

		public LocText LocLabel => default;
		public string Label { get; }
		public event Action LabelChanged;

		public bool? Interactable => true;
		public event Action<bool> InteractableChanged;

		public string Style => null;
		public event Action<string> StyleChanged;

		public Action Action => null;
	}

	public class UIButton : UIWidget<UIButtonLayout, IButtonViewModel>, IClickable<UIButton>
	{
		private Sprite _defaultIconSprite;
		private string _defaultLabelText;

		private string _currentStyle;

		protected UISpriteAssigner _spriteAssigner;
		protected UITextLocalizationAssigner _localizationAssigner;

		public event Action<UIButton> Clicked;

		public UIButton() : base()
		{
		}

		public UIButton(UIButtonLayout layout) : base(layout)
		{
		}

		protected internal sealed override void OnLayoutInstalledInternal()
		{
			Create(out _spriteAssigner);
			Create(out _localizationAssigner);

			if (!_layout.disableDefaultIcon && _layout.icon)
				_defaultIconSprite = _layout.icon.sprite;

			if (!_layout.disableDefaultLabel && _layout.label)
				_defaultLabelText = _layout.label.text;

			_layout.Subscribe(OnClicked);

			base.OnLayoutInstalledInternal();
		}

		protected internal sealed override void OnLayoutClearedInternal()
		{
			_spriteAssigner.Dispose();
			_localizationAssigner.Dispose();

			Clicked = null;

			_layout.button.Unsubscribe(OnClicked);

			base.OnLayoutClearedInternal();
		}

		protected override void OnShow(ref IButtonViewModel vm)
		{
			UpdateIcon();
			vm.IconChanged += UpdateIcon;

			UpdateLabel();
			vm.LabelChanged += UpdateLabel;

			SetInteractable(vm.Interactable ?? true);
			vm.InteractableChanged += SetInteractable;

			SetStyle(vm.Style);
			vm.StyleChanged += SetStyle;
		}

		protected override void OnHide(ref IButtonViewModel vm)
		{
			vm.IconChanged -= UpdateIcon;
			vm.LabelChanged -= UpdateLabel;
			vm.InteractableChanged -= SetInteractable;
			vm.StyleChanged -= SetStyle;
		}

		private void UpdateIcon()
		{
			_layout.icon.SetSpriteSafe(_spriteAssigner, args.IconReference, args.Icon, _defaultIconSprite, UpdateIconActive);
		}

		private void UpdateLabel()
		{
			var locLabel = args.LocLabel.IsEmpty() && _layout.locInfo
				? _layout.locInfo.value
				: args.LocLabel;
			_layout.label.SetTextSafe(_localizationAssigner, locLabel, args.Label, _defaultLabelText, UpdateLabelActive);
		}

		public void SetLabel(string label)
		{
			_layout.label.text = label;
			UpdateLabelActive();
		}

		public void SetIcon(Sprite icon)
		{
			_layout.icon.sprite = icon;
			UpdateIconActive();
		}

		public void SetStyle(string style)
		{
			if (_currentStyle.IsNullOrEmpty() && style.IsNullOrEmpty())
				return;

			if (_currentStyle == style)
				return;

			if (_layout.styleSwitcher)
				_layout.styleSwitcher.Switch(style);

			_currentStyle = style;
		}

		protected virtual void OnClicked()
		{
			_args.Action?.Invoke();
			Clicked?.Invoke(this);
		}

		internal void Subscribe(Action<UIButton> action) => Clicked += action;

		internal void Unsubscribe(Action<UIButton> action) => Clicked -= action;

		internal void SetInteractable(bool value) =>
			_layout.button.interactable = value;

		private void UpdateLabelActive()
		{
			var active = !_layout.label.text.IsNullOrWhiteSpace();
			_layout.labelGroup.SetActiveSafe(active);
		}

		private void UpdateIconActive()
		{
			bool active = _layout.icon.sprite;
			_layout.iconGroup.SetActiveSafe(active);
		}
	}

	public static class UIButtonWidget2Extensions
	{
		/// <summary>
		/// Отписку (<see cref="Unsubscribe"/>) можно не делать так как при очистке верстки, произойдет очистка подписчиков
		/// </summary>
		public static void Subscribe(this UIButton button, Action<UIButton> action) => button.Subscribe(action);

		public static void Unsubscribe(this UIButton button, Action<UIButton> action) => button.Unsubscribe(action);

		public static void SetInteractable(this UIButton button, bool value) => button.SetInteractable(value);
	}
}
