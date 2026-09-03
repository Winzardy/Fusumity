using System.Collections.Generic;
using System;
using Localization;
using Sapientia.Collections;

namespace UI
{
	public static class PriceBannerState
	{
		public const string AVAILABLE = "Available";
		public const string LOADING = "Loading";
	}

	public class UIPriceWidget : UIWidget<UIPriceWidgetLayout, UIPriceWidget.Args>
	{
		public struct Args : IEquatable<Args>
		{
			public UIObsoleteLabeledIconWidget.Args? banner;
			public UIObsoleteLabeledIconWidget.Args[] items;

			/// <summary>
			/// Блок-группа предметов над кнопкой
			/// </summary>
			public UIObsoleteLabeledIconWidget.Args[] miniItems;

			public LocText locBadge;
			public string badge;

			public LocText locLabel;
			public string label;
			public bool Equals(Args other)
				=> badge == other.badge &&
					label == other.label &&
					locBadge.Equals(other.locBadge) &&
					locLabel.Equals(other.locLabel) &&
					EqualityComparer<UIObsoleteLabeledIconWidget.Args?>.Default.Equals(banner, other.banner) &&
					items.SequenceEquals(other.items) &&
					miniItems.SequenceEquals(other.miniItems);

			public override bool Equals(object obj) => obj is Args other && Equals(other);

			public override int GetHashCode()
				=> HashCode.Combine(badge, label, locBadge, locLabel, banner, items?.Length ?? 0, miniItems?.Length ?? 0);
		}

		private UIObsoleteLabeledIconWidget _banner;

		private UIObsoleteLabeledIconWidget _single;
		private UIGroup<UIObsoleteLabeledIconWidget, UIObsoleteLabeledIconWidgetLayout, UIObsoleteLabeledIconWidget.Args> _group;
		private UIGroup<UIObsoleteLabeledIconWidget, UIObsoleteLabeledIconWidgetLayout, UIObsoleteLabeledIconWidget.Args> _miniGroup;

		private UITextLocalizationAssigner _localizationAssigner;

		protected override void OnLayoutInstalled()
		{
			Create(out _localizationAssigner);

			TryCreateWidget(ref _banner, _layout.banner);
			TryCreateWidget(ref _single, _layout.item);
			TryCreateWidget(ref _miniGroup, _layout.miniGroup);
			TryCreateWidget(ref _group, _layout.group);
		}

		protected override void OnLayoutCleared()
		{
			_localizationAssigner.Dispose();
		}

		protected override void OnShow(ref Args args)
		{
			if (!args.items.IsNullOrEmpty())
			{
				if (args.items.Length > 1)
				{
					_single?.Hide(immediate: true);
					_group?.Show(args.items, _immediate);
				}
				else
				{
					_single?.Show(args.items.First(), _immediate);
					_group?.Hide(immediate: true);
				}
			}
			else
			{
				_single?.Hide(immediate: true);
				_group?.Hide(immediate: true);
			}

			if (!args.miniItems.IsNullOrEmpty())
				_miniGroup?.Show(args.miniItems, _immediate);
			else
				_miniGroup?.Hide(immediate: true);

			if (args.banner.HasValue)
				_banner.Show(args.banner.Value, _immediate);
			else
				_banner?.Hide(immediate: true);

			_layout.label.SetTextOrDeactivateSafe(_localizationAssigner, args.locLabel, args.label);
			_layout.badge.SetTextOrDeactivateSafe(_localizationAssigner, args.locBadge, args.badge);
		}
	}
}
