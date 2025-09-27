using AssetManagement;
using Localization;
using UnityEngine;

namespace UI
{
	public class UILabeledIconWidget : UIWidget<UILabeledIconWidgetLayout, UILabeledIconWidget.Args>
	{
		public struct Args
		{
			public AssetReferenceEntry<Sprite> iconRef;
			public Sprite icon;

			public LocText locLabel;
			public string label;

			public string state;
		}

		private Sprite _defaultIconSprite;
		private string _defaultLabelText;

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
		}

		protected override void OnLayoutCleared()
		{
			_spriteAssigner.Dispose();
			_localizationAssigner.Dispose();
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
		}
	}
}
