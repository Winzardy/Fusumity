namespace UI
{
	public class UIMarkerGroupC<TWidget, TLayout, TValue> : UIGroup<UIMarkerC<TWidget, TLayout, TValue>, UIMarkerLayout,
		UIMarkerArgs<WidgetСArgs<TValue>>>
		where TWidget : UIWidgetC<TLayout, TValue>
		where TLayout : UIBaseLayout
		where TValue : class
	{
	}

	public class UIMarkerGroup<TWidget, TLayout, TArgs> : UIGroup<UIMarker<TWidget, TLayout, TArgs>, UIMarkerLayout, UIMarkerArgs<TArgs>>
		where TWidget : UIWidget<TLayout, TArgs>
		where TLayout : UIBaseLayout
		where TArgs : struct
	{
	}
}
