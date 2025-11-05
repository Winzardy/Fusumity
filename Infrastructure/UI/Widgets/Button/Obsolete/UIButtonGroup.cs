using System;

namespace UI
{
	public class UIButtonGroup : UIButtonGroup<UIButtonWidget, UILabeledButtonLayout, UIButtonWidget.Args>
	{
	}

	public class UIButtonGroup<TButtonWidget, TButtonLayout, TButtonArgs> :
		UIGroup<TButtonWidget, TButtonLayout, TButtonArgs>
		where TButtonWidget : UIButtonWidget<TButtonLayout, TButtonArgs>
		where TButtonLayout : UILabeledButtonLayout
		where TButtonArgs : struct, IObseleteButtonViewModel
	{
		public event Action<TButtonWidget, int> Clicked;

		protected override void OnRegisteredElement(TButtonWidget widget) => widget.SetOverrideActionOnClick(OnClicked);

		private void OnClicked(IButtonWidget widget)
		{
			var button = (TButtonWidget) widget;
			var index = this[button];
			button.args.Action?.Invoke();
			Clicked?.Invoke(button, index);
		}
	}
}
