using AssetManagement;
using Localizations;
using UnityEngine;

namespace UI
{
	public class UILabeledIconWidget : UIWidget<UILabeledIconWidgetLayout, UILabeledIconWidget.Args>
	{
		public struct Args
		{
			public AssetReferenceEntry<Sprite> iconReference;
			public Sprite icon;
			public TextLocalizationArgs labelLocArgs;
			public string label;
			public TextLocalizationArgs additionLabelLocArgs;
			public string additionLabel;

			// TODO: убрать как вернусь из отпуска
			public bool? state;
		}

		private Sprite _defaultIconSprite;
		private string _defaultLabelText;
		private string _additionLabelText;

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
			if(_layout.additionLabel)
				_additionLabelText = _layout.additionLabel.text;
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
				args.iconReference,
				args.icon,
				_defaultIconSprite
			);
			_layout.label.SetTextSafe
			(
				_localizationAssigner,
				args.labelLocArgs,
				args.label,
				_defaultLabelText
			);
			_layout.additionLabel.SetTextSafe
			(
				_localizationAssigner,
				args.additionLabelLocArgs,
				args.additionLabel,
				_defaultLabelText
			);

			// TODO: убрать как вернусь из отпуска
			_layout.stateSwitcher?.Switch(args.state ?? false);
		}
	}
}
