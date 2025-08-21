using System;
using Content;
using Sapientia.Pooling;
using UnityEngine;

namespace UI.Popovers
{
	internal interface IPopoverPool : IDisposable
	{
	}

	internal class PopoverPool<T> : ObjectPool<T>, IPopoverPool
		where T : UIWidget, IPopover
	{
		private bool _capacityEnsured;

		internal PopoverPool(UIPopoverFactory factory) : base(new Policy(factory))
		{
		}

		internal T Get(UIWidget host, RectTransform customAnchor = null)
		{
			var popover = Get();
			popover.Attach(host, customAnchor);
			EnsureCapacity(popover.Id);
			return popover;
		}

		// Костыль, достать Entry без Id сложно, в C# 11 появилось static abstract... оно бы это решило
		private void EnsureCapacity(string entryId)
		{
			if(_capacityEnsured)
				return;

			var entry = ContentManager.Get<UIPopoverEntry>(entryId);
			if (entry.poolCapacity)
				SetCapacity(entry.poolCapacity);

			_capacityEnsured = true;
		}

		private class Policy : IObjectPoolPolicy<T>
		{
			private readonly UIPopoverFactory _factory;

			public Policy(UIPopoverFactory factory)
			{
				_factory = factory;
			}

			public T Create() => _factory.Create<T>();

			public void OnGet(T obj)
			{
			}

			public void OnRelease(T obj)
			{
				obj.Detach();
			}

			public void OnDispose(T obj)
			{
				obj.Dispose();
			}
		}
	}
}
