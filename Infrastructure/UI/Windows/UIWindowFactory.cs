using System;
using Content;

namespace UI.Windows
{
	public class UIWindowFactory
	{
		public T Create<T>()
			where T : UIWidget, IWindow
		{
			var window = UIFactory.CreateWidget<T>(false);
			return Initialize(window);
		}

		public IWindow Create(Type type)
		{
			if (UIFactory.CreateWidget(type, false) is not IWindow window)
				throw GUIDebug.Exception($"Error create screen by type [ {type.Name} ]");

			return Initialize(window);
		}

		private T Initialize<T>(T window)
			where T : IWindow
		{
			var entry = ContentManager.Get<UIWindowEntry>(window.Id);
			window.Initialize(entry);
			return window;
		}
	}
}
