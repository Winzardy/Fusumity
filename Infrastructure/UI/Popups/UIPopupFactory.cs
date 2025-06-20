using System;
using Content;

namespace UI.Popups
{
	public class UIPopupFactory
	{
		public T Create<T>()
			where T : UIWidget, IPopup
		{
			var popup = UIFactory.CreateWidget<T>(false);
			return Initialize(popup);
		}

		public IPopup Create(Type type)
		{
			if (UIFactory.CreateWidget(type, false) is not IPopup popup)
				throw GUIDebug.Exception($"Error create popup by type [ {type.Name} ]");

			return Initialize(popup);
		}

		private T Initialize<T>(T popup)
			where T : IPopup
		{
			var entry = ContentManager.Get<UIPopupEntry>(popup.Id);
			popup.Initialize(entry);
			return popup;
		}
	}
}
