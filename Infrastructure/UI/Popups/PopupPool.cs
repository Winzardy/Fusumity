using System;
using System.Collections.Generic;

namespace UI.Popups
{
	public class PopupPool : IDisposable
	{
		private readonly UIPopupFactory _factory;

		private Dictionary<Type, IPopupPool> _pools = new(4);

		public PopupPool(UIPopupFactory factory)
		{
			_factory = factory;
		}

		public void Dispose()
		{
			foreach (var pool in _pools.Values)
				pool.Dispose();

			_pools = null;
		}

		public T Get<T>()
			where T : UIWidget, IPopup
		{
			var pool = GetOrCreatePool<T>();
			return pool.Get();
		}

		public void Release(IPopup popup)
		{
			var pool = _pools[popup.GetType()];
			pool.Release(popup);
		}

		private PopupPool<T> GetOrCreatePool<T>()
			where T : UIWidget, IPopup
		{
			if (_pools.TryGetValue(typeof(T), out var rawPool))
				return (PopupPool<T>) rawPool;

			var pool = new PopupPool<T>(_factory);
			_pools[typeof(T)] = pool;
			return pool;
		}
	}
}
