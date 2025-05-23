using System;
using System.Collections.Generic;

namespace UI.Popovers
{
	public class PopoverPool : IDisposable
	{
		private UIPopoverFactory _factory;
		private int _maxSize;

		private Dictionary<Type, IPopoverObjectPool> _pools = new(8);

		public PopoverPool(UIPopoverFactory factory, int maxSize = 0)
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
			where T : UIWidget, IPopover
		{
			if (TryGetPool<T>(out var pool))
				return pool.Get();

			pool = new PopoverObjectPool<T>(_factory, maxSize: _maxSize);
			_pools[typeof(T)] = pool;

			return pool.Get();
		}

		public void Release(IPopover popup)
		{
			if (_pools.TryGetValue(popup.GetType(), out var pool))
			{
				pool.Release(popup);
			}
		}

		private bool TryGetPool<T>(out PopoverObjectPool<T> objectPool)
			where T : UIWidget, IPopover
		{
			objectPool = default;

			if (_pools.TryGetValue(typeof(T), out var value))
			{
				objectPool = (PopoverObjectPool<T>) value;
				return true;
			}

			return false;
		}
	}
}
