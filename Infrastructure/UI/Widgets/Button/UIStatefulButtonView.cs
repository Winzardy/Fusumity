using System;
using ActionBusSystem;
using Fusumity.MVVM;
using Fusumity.MVVM.UI;
using Fusumity.Utility;
using Game.UI;
using JetBrains.Annotations;
using Sapientia.Extensions;
using UnityEngine;

namespace UI
{
	public class UIStatefulButtonView : UIView<IStatefulButtonViewModel, UIStatefulButtonLayout>, IClickable<UIStatefulButtonView>
	{
		private UIAdBannerView _adBanner;
		private UILabeledIconView _labeledIcon;
		private UIAttentionIndicatorView _indicator;
		private UISpriteAssigner _assigner;

		private ActionBusElement _clickElement;

		public event Action<UIStatefulButtonView> Clicked;

		public UIStatefulButtonView(UIStatefulButtonLayout layout) : base(layout)
		{
			if (layout.adBanner != null)
			{
				AddDisposable(_adBanner = new UIAdBannerView(layout.adBanner));
			}

			if (layout.labeledIcon != null)
			{
				AddDisposable(_labeledIcon = new UILabeledIconView(layout.labeledIcon, true));
			}

			if (layout.indicator != null)
			{
				AddDisposable(_indicator = new UIAttentionIndicatorView(layout.indicator));
			}

			TryRegisterClickAndSubscribe(layout);
		}

		protected override void OnUpdate(IStatefulButtonViewModel viewModel)
		{
			SetActive(true);

			if (viewModel is IUniqueStatefulButtonViewModel unique)
				TryRegisterClickAndSubscribe(_layout, unique.Id, unique.GroudId);
			else
				TryRegisterClickAndSubscribe(_layout);

			if (_layout.label != null && viewModel.Label != null)
				viewModel.Label.Bind(UpdateLabel);

			UpdateBanner();

			_labeledIcon?.Update(viewModel.LabeledIcon);
			_indicator?.Update(viewModel.Indicator);

			UpdateIcon();
			UpdateStyle();
			UpdateInteractable();

			viewModel.StyleChanged += UpdateStyle;
			viewModel.InteractableChanged += UpdateInteractable;
		}

		protected override void OnClear(IStatefulButtonViewModel viewModel)
		{
			if (viewModel is IUniqueStatefulButtonViewModel)
				TryRegisterClickAndSubscribe(_layout);

			viewModel.Label?.Release();

			viewModel.StyleChanged -= UpdateStyle;
			viewModel.InteractableChanged -= UpdateInteractable;
		}

		private void TryRegisterClickAndSubscribe(UIStatefulButtonLayout layout, string uId = null, string groupId = null)
		{
			uId = uId.IsNullOrEmpty() ? layout.uId : uId;
			groupId = groupId.IsNullOrEmpty() ? layout.groupId : groupId;

			if (_clickElement != null)
			{
				if (_clickElement.Matches(uId, groupId))
					return;

				DisposeAndRemoveDisposable(_clickElement);
			}

			_clickElement = null;

			if (layout == null)
				return;

			_clickElement = Subscribe(layout.button, HandleClick, uId, groupId);
		}

		protected override void OnNullViewModel()
		{
			SetActive(false);
		}

		protected internal void UpdateBanner()
		{
			_adBanner?.Update(ViewModel.AdBanner);
		}

		private void UpdateLabel(string text)
		{
			_layout.label.text = text;
			var active = !text.IsNullOrEmpty();
			_layout.label.SetActive(active);
			_layout.labelGroup.SetActiveSafe(active);
		}

		private void UpdateStyle()
		{
			SetStyle(ViewModel.Style);
		}

		private void UpdateInteractable()
		{
			_layout.button.interactable = !ViewModel.Interactable.HasValue || ViewModel.Interactable.Value;
		}

		private void UpdateIcon()
		{
			if (_layout.icon == null)
			{
				_layout.iconGroup.SetActive(false);
				return;
			}

			if (ViewModel.Icon.IsEmptyOrInvalid())
				return;

			if (_assigner == null)
			{
				AddDisposable(_assigner = new UISpriteAssigner());
			}

			_layout.iconGroup.SetActive(true);
			_assigner.TrySetSprite(_layout.icon, ViewModel.Icon);
		}

		public void IgnoreLocking(bool ignore)
		{
			if (_clickElement == null || _clickElement.IsDisposed)
				return;

			_clickElement.IsLockResistant = ignore;
		}

		public void SetStyle(string style)
		{
			if (_layout.switcher != null)
				_layout.switcher.Switch(style);
		}

		private void HandleClick()
		{
			Clicked?.Invoke(this);
			ViewModel?.Click();
		}
	}

	public interface IUniqueStatefulButtonViewModel : IStatefulButtonViewModel
	{
		[CanBeNull] string Id { get; }
		[CanBeNull] string GroudId { get => null; }
	}

	public interface IStatefulButtonViewModel
	{
		ILabelViewModel Label { get; }
		string Style { get; }

		bool? Interactable { get => null; }
		UISpriteInfo Icon { get => default; }
		IAdBannerViewModel AdBanner { get => null; }
		ILabeledIconViewModel LabeledIcon { get => null; }
		IStatefulViewModel Indicator { get => null; }

		event Action StyleChanged;
		event Action InteractableChanged;

		void Click();
	}
}
