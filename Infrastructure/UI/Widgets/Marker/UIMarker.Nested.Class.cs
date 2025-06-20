namespace UI
{
	public class UIMarkerC<TWidget, TLayout, TValue> : UIMarker<TWidget, TLayout, WidgetСArgs<TValue>>
		where TWidget : UIWidgetC<TLayout, TValue>
		where TLayout : UIBaseLayout
		where TValue : class
	{
	}
}
