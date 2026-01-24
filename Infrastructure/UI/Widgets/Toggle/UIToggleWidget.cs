using System;
using AssetManagement;
using Content;
using Localization;
using UnityEngine;

namespace UI
{
	/// <summary>
	/// Дефолтный виджет, для сложных кейсов есть Generic
	/// </summary>
	public class UIToggleWidget : UIToggleWidget<UIToggleButtonLayout, UIToggleWidget.Args>
	{
		public struct Args : IToggleArgs
		{
			public bool IsOn { get; set; }

			public AssetReferenceEntry<Sprite> IconReference { get; set; }

			public Sprite Icon { get; set; }

			public LocText LocLabel { get; set; }

			public string Label { get; set; }

			public bool? Interactable { get; set; }
		}
	}

	public interface IToggleArgs : IObseleteButtonViewModel
	{
		public bool IsOn { get; set; }

		public bool AutoToggleOnClick => true;
	}

	public interface IToggleWidget : IButtonWidget
	{
		public void Toggle(bool enable, bool immediate);
	}

	public class UIToggleWidget<TLayout, TArgs> : UIButtonWidget<TLayout, TArgs>, IToggleWidget
		where TLayout : UIToggleButtonLayout
		where TArgs : struct, IToggleArgs
	{
		public bool IsOn => _args.IsOn;

		public event Action<IToggleWidget, bool> Toggled;

		protected override void OnSetupDefaultAnimator()
			=> SetAnimator<DefaultToggleWidgetAnimator>();

		protected override void OnShow(ref TArgs args)
		{
			ToggleInternal(args.IsOn, true);

			base.OnShow(ref args);
		}

		public void Toggle(bool isOn, bool immediate = false)
		{
			if (!immediate && _args.IsOn == isOn)
				return;

			_args.IsOn = isOn;

			ToggleInternal(isOn, immediate);

			Toggled?.Invoke(this, isOn);
		}


		private void ToggleInternal(bool isOn, bool immediate = false)
		{
			if (_layout.activitySwitcher)
				_layout.activitySwitcher.Switch(isOn);

			var key = isOn ? WidgetAnimationType.TOGGLE_ENABLING : WidgetAnimationType.TOGGLE_DISABLING;
			_animator.Play(key, immediate);
		}

		protected override void OnClicked()
		{
			if (_overrideActionOnClicked != null)
			{
				_overrideActionOnClicked?.Invoke(this);
				return;
			}

			if (_args.AutoToggleOnClick)
			{
				var value = !_args.IsOn;
				Toggle(value);
			}

			base.OnClicked();
		}
	}
}
