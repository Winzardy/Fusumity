using System;
using Sapientia.Pooling;

namespace UI.Popovers
{
	internal interface IPopoverPool : IDisposable
	{
	}

	internal class PopoverPool<T> : ObjectPool<T>, IPopoverPool
		where T : UIWidget, IPopover
	{
		internal PopoverPool(UIPopoverFactory factory) : base(new Policy(factory))
		{
		}

		internal T Get(UIWidget anchor)
		{
			var popover = Get();
			popover.Bind(anchor);
			return popover;
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
				obj.Unbind();
			}

			public void OnDispose(T obj)
			{
				obj.Dispose();
			}
		}
	}
}
