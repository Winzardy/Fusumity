using AssetManagement;
using Fusumity.MVVM.UI;
using System;
using UnityEngine;

namespace UI
{
	public class UIAdBannerView : UIView<IAdBannerViewModel, UIAdBannerLayout>
	{
		private UISpriteAssigner _assigner;

		public UIAdBannerView(UIAdBannerLayout layout) : base(layout)
		{
		}

		protected override void OnUpdate(IAdBannerViewModel viewModel)
		{
			SetActive(true);

			UpdateIcon();
			UpdateLabel();
			UpdateStyle();

			viewModel.LabelChanged += UpdateLabel;
			viewModel.StyleChanged += UpdateStyle;
		}

		protected override void OnClear(IAdBannerViewModel viewModel)
		{
			viewModel.LabelChanged -= UpdateLabel;
			viewModel.StyleChanged -= UpdateStyle;
		}

		protected override void OnNullViewModel()
		{
			SetActive(false);
		}

		private void UpdateIcon()
		{
			if (ViewModel.Icon.IsEmptyOrInvalid())
				return;

			if (_assigner == null)
			{
				AddDisposable(_assigner = new UISpriteAssigner());
			}

			_assigner.TrySetSprite(_layout.icon, ViewModel.Icon);
		}

		private void UpdateLabel()
		{
			_layout.label.text = ViewModel.Label;
		}

		private void UpdateStyle()
		{
			_layout.switcher?.Switch(ViewModel.Style);
		}
	}

	public interface IAdBannerViewModel
	{
		string Label { get; }
		string Style { get; }
		UISpriteInfo Icon { get => default; }

		event Action LabelChanged;
		event Action StyleChanged;
	}
}
