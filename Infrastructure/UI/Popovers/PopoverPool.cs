using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Popovers
{
	public class PopoverPool : IDisposable
	{
		private readonly UIPopoverFactory _factory;

		private Dictionary<Type, IPopoverPool> _pools = new(4);

		public PopoverPool(UIPopoverFactory factory)
		{
			_factory = factory;
		}

		public void Dispose()
		{
			foreach (var pool in _pools.Values)
				pool.Dispose();

			_pools = null;
		}

		public T Get<T>(UIWidget host, RectTransform customAnchor = null)
			where T : UIWidget, IPopover
		{
			var pool = GetOrCreatePool<T>();
			return pool.Get(host, customAnchor);
		}

		public void Release<T>(T popover)
			where T : UIWidget, IPopover
		{
			var pool = GetPool<T>();
			pool.Release(popover);
		}

		private PopoverPool<T> GetOrCreatePool<T>()
			where T : UIWidget, IPopover
		{
			if (_pools.TryGetValue(typeof(T), out var rawPool))
				return (PopoverPool<T>) rawPool;


			var pool = new PopoverPool<T>(_factory);
			_pools[typeof(T)] = pool;
			return pool;
		}

		private PopoverPool<T> GetPool<T>()
			where T : UIWidget, IPopover =>
			(PopoverPool<T>) _pools[typeof(T)];
	}
}
