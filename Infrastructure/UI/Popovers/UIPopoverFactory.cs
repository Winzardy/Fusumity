using Content;

namespace UI.Popovers
{
	public class UIPopoverFactory
	{
		public T Create<T>()
			where T : UIWidget, IPopover
		{
			var tooltip = UIFactory.CreateWidget<T>(false);
			var entry = ContentManager.Get<UIPopoverEntry>(tooltip.Id);

			tooltip.Initialize(entry);

			return tooltip;
		}
	}
}
