using Localizations;
using Sapientia.Collections;
using UI;

public class UIPriceWidget : UIWidget<UIPriceWidgetLayout, UIPriceWidget.Args>
{
	public struct Args
	{
		public UILabeledIconWidget.Args? banner;
		public UILabeledIconWidget.Args[] items;

		/// <summary>
		/// Блок-группа предметов над кнопкой
		/// </summary>
		public UILabeledIconWidget.Args[] miniItems;

		public TextLocalizationArgs badgeLocArgs;
		public string badge;
	}

	private UILabeledIconWidget _banner;

	private UILabeledIconWidget _single;
	private UIGroup<UILabeledIconWidget, UILabeledIconWidgetLayout, UILabeledIconWidget.Args> _group;
	private UIGroup<UILabeledIconWidget, UILabeledIconWidgetLayout, UILabeledIconWidget.Args> _miniGroup;

	private UITextLocalizationAssigner _localizationAssigner;

	protected override void OnLayoutInstalled()
	{
		Create(out _localizationAssigner);

		CreateWidget(out _banner, _layout.banner);
		CreateWidget(out _single, _layout.item);
	}

	protected override void OnLayoutCleared()
	{
		_localizationAssigner.Dispose();
	}

	protected override void OnShow(ref Args args)
	{
		if (!args.items.IsNullOrEmpty())
		{
			if (args.items.Length >= 1)
			{
				_single.Hide(immediate: true);
				TryCreateWidget(ref _group, _layout.group);
				_group?.Show(args.items, _immediate);
			}
			else
			{
				_single.Show(args.items.First(), _immediate);
				_group?.Hide(immediate: true);
			}
		}
		else
		{
			_single.Hide(immediate: true);
			_group?.Hide(immediate: true);
		}

		if (!args.miniItems.IsNullOrEmpty())
		{
			TryCreateWidget(ref _miniGroup, _layout.miniGroup);
			_miniGroup?.Show(args.items, _immediate);
		}
		else
		{
			_miniGroup?.Hide(immediate: true);
		}

		if (args.banner.HasValue)
			_banner.Show(args.banner.Value, _immediate);
		else
			_banner.Hide(immediate: true);

		_layout.badge.TrySetTextOrDeactivate(_localizationAssigner, args.badgeLocArgs, args.badge);
	}
}
