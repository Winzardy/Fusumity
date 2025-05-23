using System;
using Sapientia.Pooling;

namespace UI.Popups
{
	public interface IPopupPool : IDisposable
	{
		public void Release(IPopup popup);
	}

	public class PopupPool<T> : ObjectPool<T>, IPopupPool
		where T : UIWidget, IPopup
	{
		public PopupPool(UIPopupFactory factory, bool collectionCheck = false, int capacity = 0, int maxSize = 0)
			: base(new Policy(factory), collectionCheck, capacity, maxSize)
		{
		}

		void IPopupPool.Release(IPopup popup)
		{
			Release((T) popup);
		}

		private class Policy : IObjectPoolPolicy<T>
		{
			private UIPopupFactory _factory;

			public Policy(UIPopupFactory factory)
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
