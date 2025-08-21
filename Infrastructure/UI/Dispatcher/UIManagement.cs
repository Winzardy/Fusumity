using System;
using System.Collections.Generic;
using Content;
using Fusumity.Utility;
using UI.Layers;

namespace UI
{
	internal class UIDispatcherLocator<T>
		where T : class, IWidgetDispatcher
	{
		internal static T instance;
	}

	public class UIManagement : IDisposable
	{
		private const string NAME_FORMAT = "[Canvas] {0}";

		private Dictionary<string, UILayerLayout> _layers = new();

		public UILayerLayout this[string id] => _layers.TryGetValue(id, out var layer) ? layer : Create(id);

		public bool TryGet(string id, out UILayerLayout layer) => _layers.TryGetValue(id, out layer);

		public T Get<T>() where T : class, IWidgetDispatcher
			=> UIDispatcherLocator<T>.instance;

		public void Register<T>(T dispatcher)
			where T : class, IWidgetDispatcher
			=> UIDispatcherLocator<T>.instance = dispatcher;

		public void Unregister<T>()
			where T : class, IWidgetDispatcher
			=> UIDispatcherLocator<T>.instance = null;

		private UILayerLayout Create(string id)
		{
			var entry = ContentManager.Get<UILayerEntry>(id);
			var layout = UIFactory.CreateLayout(entry.template);

			layout.MoveTo(UIFactory.scene);

			layout.name = string.Format(NAME_FORMAT, id);
			layout.canvas.sortingOrder = entry.sortOrder;
			_layers[id] = layout;

			return layout;
		}

		public void Dispose()
		{
			_layers = null;
		}
	}
}
