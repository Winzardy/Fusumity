using System;
using AssetManagement;
using Localization;
using UnityEngine;

namespace UI
{
	[Obsolete("Лучше использовать UILabeledIcon")]
	public class UIObsoleteLabeledIconWidget : UIWidget<UIObsoleteLabeledIconWidgetLayout, UIObsoleteLabeledIconWidget.Args>
	{
		public struct Args
		{
			public AssetReference<Sprite> iconRef;
			public Sprite icon;

			public LocText locLabel;
			public string label;

			public string state;

			public Action onClick;
			public Action<RectTransform> onClickRT;
		}

		private Sprite _defaultIconSprite;
		private string _defaultLabelText;
		private Action _onClick;
		private Action<RectTransform> _onClickRT;

		private UITextLocalizationAssigner _localizationAssigner;
		private UISpriteAssigner _spriteAssigner;

		protected override void OnLayoutInstalled()
		{
			Create(out _spriteAssigner);
			Create(out _localizationAssigner);

			_localizationAssigner.SetTextSafe(_layout);

			if (_layout.icon)
				_defaultIconSprite = _layout.icon.sprite;
			if (_layout.label)
				_defaultLabelText = _layout.label.text;

			if(_layout.button != null)
			{
				_layout.button.Subscribe(HandleClicked);
			}
		}

		protected override void OnLayoutCleared()
		{
			_spriteAssigner.Dispose();
			_localizationAssigner.Dispose();

			_onClick = null;
			_onClickRT = null;
			if (_layout.button != null)
			{
				_layout.button.Unsubscribe(HandleClicked);
			}
		}

		protected override void OnShow(ref Args args)
		{
			_layout.icon.SetSpriteSafe
			(
				_spriteAssigner,
				args.iconRef,
				args.icon,
				_defaultIconSprite
			);
			_layout.label.SetTextSafe
			(
				_localizationAssigner,
				args.locLabel,
				args.label,
				_defaultLabelText
			);

			_layout.stateSwitcher?.Switch(args.state);
			_onClick = args.onClick;
			_onClickRT = args.onClickRT;
		}

		private void HandleClicked()
		{
			_onClick?.Invoke();
			_onClickRT?.Invoke(RectTransform);
		}
	}
}
