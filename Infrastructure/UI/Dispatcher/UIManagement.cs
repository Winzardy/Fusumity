using Content;
using Fusumity.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Layers;

namespace UI
{
	internal class UIDispatcherLocator<T>
		where T : class, IWidgetDispatcher
	{
		internal static T instance;
	}

	public class UIManagement : IDisposable, IEnumerable<UILayerLayout>
	{
		private const string NAME_FORMAT = "[Canvas] {0}";

		private Dictionary<string, UILayerLayout> _layers = new();

		private readonly List<IWidgetDispatcher> _dispatchers = new();

		public UILayerLayout this[string id] => _layers.TryGetValue(id, out var layer) ? layer : Create(id);

		public bool TryGet(string id, out UILayerLayout layer) => _layers.TryGetValue(id, out layer);

		public T Get<T>() where T : class, IWidgetDispatcher
			=> UIDispatcherLocator<T>.instance;

		public void Register<T>(T dispatcher)
			where T : class, IWidgetDispatcher
		{
			var previous = UIDispatcherLocator<T>.instance;
			if (previous != null)
				_dispatchers.Remove(previous);

			UIDispatcherLocator<T>.instance = dispatcher;
			_dispatchers.Add(dispatcher);
		}

		public void Unregister<T>()
			where T : class, IWidgetDispatcher
		{
			_dispatchers.Remove(UIDispatcherLocator<T>.instance);
			UIDispatcherLocator<T>.instance = null;
		}

		public void HideAll()
		{
			for (var i = _dispatchers.Count - 1; i >= 0; i--)
				_dispatchers[i].TryHideAll();
		}

		private UILayerLayout Create(string id)
		{
			var entry = ContentManager.Get<UILayerConfig>(id);
			var layout = UIFactory.CreateLayout(entry.template);

			layout.MoveToScene(UIFactory.scene);

			layout.name = string.Format(NAME_FORMAT, id);
			layout.canvas.sortingOrder = entry.sortOrder;
			_layers[id] = layout;

			return layout;
		}

		public void Dispose()
		{
			foreach (var layout in _layers.Values)
				layout.Destroy();

			_layers.Clear();
			_dispatchers.Clear();
		}

		public IEnumerator<UILayerLayout> GetEnumerator() => _layers.Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
