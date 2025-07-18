using System;
using AssetManagement;
using Fusumity.Utility;
using Localization;
using Sapientia.Extensions;
using TMPro;
using UnityEngine;

namespace UI
{
	/// <summary>
	/// Дефолтный виджет, для сложных кейсов есть Generic
	/// </summary>
	public class UIButtonWidget : UIButtonWidget<UILabeledButtonLayout, UIButtonWidget.Args>
	{
		public struct Args : IButtonArgs
		{
			public AssetReferenceEntry<Sprite> IconReference { get; set; }

			public Sprite Icon { get; set; }

			public LocText LocLabel { get; set; }

			public string Label { get; set; }

			public Action Action { get; set; }

			public bool? Interactable { get; set; }

			public string Style { get; set; }

			/// <summary>
			/// Отключить добавление префикса <see cref="UILabeledButtonLayout.ANIMATION_KEY_PREFIX"/>
			/// </summary>
			[Obsolete("убрать после полной миграции")]
			public bool DisablePrefixStyle { get; set; }
		}
	}

	public interface IButtonArgs
	{
		public AssetReferenceEntry<Sprite> IconReference { get; }

		//Лучше использовать ассет референс
		public Sprite Icon { get; }

		public LocText LocLabel { get; }

		public string Label { get; }

		public Action Action => null;
		public bool? Interactable => true;

		public string Style => null;

		public bool DisablePrefixStyle => false;
	}

	public interface IButtonWidget
	{
	}

	public class UIButtonWidget<TLayout, TArgs> : UIWidget<TLayout, TArgs>, IButtonWidget
		where TLayout : UILabeledButtonLayout
		where TArgs : struct, IButtonArgs
	{
		private Sprite _defaultIconSprite;
		private string _defaultLabelText;

		private string _currentStyle;

		private Action _action;

		private event Action _clicked;

		protected UITextLocalizationAssigner _localizationAssigner;
		protected UISpriteAssigner _spriteAssigner;

		protected Action<IButtonWidget> _overrideActionOnClicked;

		public event Action<IButtonWidget> Clicked;

		protected internal sealed override void OnLayoutInstalledInternal()
		{
			Create(out _spriteAssigner);
			Create(out _localizationAssigner)
			   .Updated += OnPlaceholderUpdated;

			_localizationAssigner.SetTextSafe(_layout);

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
			_localizationAssigner.Updated -= OnPlaceholderUpdated;

			_layout.Unsubscribe(OnClicked);

			_clicked = null;
			Clicked = null;

			base.OnLayoutClearedInternal();
		}

		protected override void OnShow(ref TArgs args)
		{
			_layout.icon.SetSpriteSafe(_spriteAssigner, args.IconReference, args.Icon, _defaultIconSprite, UpdateIconGroup);
			_layout.label.SetTextSafe(_localizationAssigner, args.LocLabel, args.Label, _defaultLabelText, UpdateLabelGroup);

			SetInteractable(args.Interactable ?? true);

			//Стили сделал через Sequence... Подумал что по сути стили это считай state кнопки.
			//Возможно это как из пушки по воробьям
			var useCustomStyle = !args.Style.IsNullOrEmpty();
			var style = !useCustomStyle ? _layout.defaultStyle : args.Style;
			var prefix = useCustomStyle && !args.DisablePrefixStyle;
			SetStyle(style, prefix);

			void UpdateIconGroup() => _layout.iconGroup.SetActiveSafe(_layout.icon.sprite);
			void UpdateLabelGroup() => _layout.labelGroup.SetActiveSafe(!_layout.label.text.IsNullOrWhiteSpace());
		}

		//Если вопрос почему internal? разная подсветка Extensions и обычных методов :P
		internal void Subscribe(Action action) => _clicked += action;

		internal void Unsubscribe(Action action) => _clicked += action;

		internal void SetAction(Action action) => _action = action;

		internal void SetInteractable(bool value) =>
			_layout.button.interactable = value;

		public void SetStyle(string style, bool prefix = true)
		{
			if (_currentStyle.IsNullOrEmpty() && style.IsNullOrEmpty())
				return;

			if (_currentStyle == style)
				return;

			if (style.IsNullOrEmpty())
			{
				if (_currentStyle == null)
					return;

				SetStyleInternal(_layout.defaultStyle, false);
				_currentStyle = null;

				return;
			}

			SetStyleInternal(style, prefix);
		}

		private void SetStyleInternal(string style, bool prefix = true)
		{
			if (_layout.styleSwitcher)
				_layout.styleSwitcher.Switch(style);
			else
			{
				//TODO: убрать это...
				var prev = GUIDebug.Logging.Widget.Animator.notFoundSequence;
				GUIDebug.Logging.Widget.Animator.notFoundSequence = false;
				var animationKey = prefix ? UILabeledButtonLayout.ANIMATION_KEY_PREFIX + style : style;
				_animator?.Play(animationKey);
				GUIDebug.Logging.Widget.Animator.notFoundSequence = prev;
			}

			_currentStyle = style;
		}

		public void SetOverrideActionOnClick(Action<IButtonWidget> action)
		{
			_overrideActionOnClicked = action;
		}

		protected virtual void OnClicked()
		{
			if (_overrideActionOnClicked != null)
			{
				_overrideActionOnClicked?.Invoke(this);
				return;
			}

			_args.Action?.Invoke();
			_action?.Invoke();

			Clicked?.Invoke(this);
			_clicked?.Invoke();
		}

		private void OnPlaceholderUpdated(TMP_Text placeholder)
		{
			if (placeholder != _layout.label)
				return;

			if (!_layout.disableLabelForceRebuild)
				_layout.label.rectTransform.ForceRebuild();
		}
	}

	public static class UIButtonWidgetExtensions
	{
		/// <summary>
		/// Отписку (<see cref="Unsubscribe"/>) можно не делать так как при очистке верстки, произойдет очистка подписчиков
		/// </summary>
		public static void Subscribe(this UIButtonWidget button, Action action) => button.Subscribe(action);

		public static void Unsubscribe(this UIButtonWidget button, Action action) => button.Unsubscribe(action);
		public static void SetAction(this UIButtonWidget button, Action action) => button.SetAction(action);
		public static void SetInteractable(this UIButtonWidget button, bool value) => button.SetInteractable(value);
	}
}
