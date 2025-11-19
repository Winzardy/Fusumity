namespace UI
{
	public class UIMarkerGroup<TWidget, TLayout, TArgs> : UIGroup<UIMarker<TWidget, TLayout, TArgs>, UIMarkerLayout, UIMarkerArgs<TArgs>>
		where TWidget : UIWidget<TLayout, TArgs>
		where TLayout : UIBaseLayout
	{
	}
}
