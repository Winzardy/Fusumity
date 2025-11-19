namespace UI
{
	public class UIMarker<TWidget, TLayout, TArgs> : UIMarker<TArgs>
		where TWidget : UIWidget<TLayout, TArgs>
		where TLayout : UIBaseLayout
	{
		private TWidget _widget;

		protected override void OnLayoutInstalled()
		{
			if (_layout.nested is not TLayout layout)
			{
				GUIDebug.LogError("Invalid nested layout!", _layout);
				return;
			}

			CreateWidget(out _widget, layout);
		}

		protected override void OnShow(ref UIMarkerArgs<TArgs> args)
			=> _widget?.Show(in args.nestedArgs, _immediate);
	}

	public class UIMarker<TWidget, TLayout> : UIMarker<EmptyArgs>
		where TWidget : UIWidget<TLayout>
		where TLayout : UIBaseLayout
	{
		private TWidget _widget;

		protected override void OnLayoutInstalled()
		{
			if (_layout.nested is not TLayout layout)
			{
				GUIDebug.LogError("Invalid nested layout!", _layout);
				return;
			}

			CreateWidget(out _widget, layout);
		}

		protected override void OnShow(ref UIMarkerArgs<EmptyArgs> args)
			=> _widget?.SetActive(true, _immediate);
	}
}
