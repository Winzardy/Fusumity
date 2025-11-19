using System;
using Content;

namespace UI.Screens
{
	public class UIScreenFactory
	{
		public T Create<T>()
			where T : UIWidget, IScreen
		{
			var screen = UIFactory.CreateWidget<T>(false);
			return Initialize(screen);
		}

		public IScreen Create(Type type)
		{
			if (UIFactory.CreateWidget(type, false) is not IScreen screen)
				throw GUIDebug.Exception($"Error create screen by type [ {type.Name} ]");

			return Initialize(screen);
		}

		private T Initialize<T>(T screen)
			where T : IScreen
		{
			var entry = ContentManager.Get<UIScreenConfig>(screen.Id);
			screen.Initialize(entry);
			return screen;
		}
	}
}
