using System;
using System.Collections.Generic;
using Content;
using Fusumity.Utility;

namespace UI.Layers
{
	public class UILayersManagement : IDisposable
	{
		private const string NAME_FORMAT = "[Canvas] {0}";

		private Dictionary<string, UILayerLayout> _layers = new();

		public UILayerLayout this[string id] => _layers.TryGetValue(id, out var layer) ? layer : Create(id);

		public void Dispose()
		{
			_layers = null;
		}

		public bool TryGet(string id, out UILayerLayout layer) => _layers.TryGetValue(id, out layer);

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
	}
}
