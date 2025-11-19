using System;
using Content;

namespace UI.Popovers
{
	public class UIPopoverFactory
	{
		public T Create<T>()
			where T : UIWidget, IPopover
		{
			var popup = UIFactory.CreateWidget<T>(false);
			return Initialize(popup);
		}

		public IPopover Create(Type type)
		{
			if (UIFactory.CreateWidget(type, false) is not IPopover popup)
				throw GUIDebug.Exception($"Error create popup by type [ {type.Name} ]");

			return Initialize(popup);
		}

		private T Initialize<T>(T popup)
			where T : IPopover
		{
			var entry = ContentManager.Get<UIPopoverConfig>(popup.Id);
			popup.Initialize(in entry);
			return popup;
		}
	}
}
