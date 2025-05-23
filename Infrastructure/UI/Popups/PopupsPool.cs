using System;
using System.Collections.Generic;

namespace UI.Popups
{
	public class PopupsPool : IDisposable
	{
		private UIPopupFactory _factory;
		private int _maxSize;

		private Dictionary<Type, IPopupPool> _pools = new(8);

		public PopupsPool(UIPopupFactory factory, int maxSize = 0)
		{
			_maxSize = maxSize;
			_factory = factory;
		}

		public void Dispose()
		{
			foreach (var pool in _pools.Values)
			{
				pool.Dispose();
			}

			_pools = null;
		}

		public T Get<T>()
			where T : UIWidget, IPopup
		{
			if (TryGetPool<T>(out var pool))
				return pool.Get();

			pool = new PopupPool<T>(_factory, maxSize: _maxSize);
			_pools[typeof(T)] = pool;

			return pool.Get();
		}

		public void Release(IPopup popup)
		{
			if (_pools.TryGetValue(popup.GetType(), out var pool))
			{
				pool.Release(popup);
			}
		}

		private bool TryGetPool<T>(out PopupPool<T> pool)
			where T : UIWidget, IPopup
		{
			pool = default;

			if (_pools.TryGetValue(typeof(T), out var value))
			{
				pool = (PopupPool<T>) value;
				return true;
			}

			return false;
		}
	}
}