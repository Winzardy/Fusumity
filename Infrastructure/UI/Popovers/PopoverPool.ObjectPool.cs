using System;
using Sapientia.Pooling;

namespace UI.Popovers
{
	public interface IPopoverObjectPool : IDisposable
	{
		public void Release(IPopover popover);
	}

	public class PopoverObjectPool<T> : ObjectPool<T>, IPopoverObjectPool
		where T : UIWidget, IPopover
	{
		public PopoverObjectPool(UIPopoverFactory factory) : base(new Policy(factory))
		{
		}

		void IPopoverObjectPool.Release(IPopover popup)
		{
			Release((T) popup);
		}

		private class Policy : IObjectPoolPolicy<T>
		{
			private UIPopoverFactory _factory;

			public Policy(UIPopoverFactory factory)
			{
				_factory = factory;
			}

			public T Create()
			{
				var popup = _factory.Create<T>();

				return popup;
			}

			public void OnGet(T obj)
			{
			}

			public void OnRelease(T obj)
			{
			}

			public void OnDispose(T obj)
			{
				obj.Dispose();
			}
		}
	}
}
