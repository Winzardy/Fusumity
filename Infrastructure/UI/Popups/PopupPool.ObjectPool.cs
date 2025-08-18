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
		public PopupPool(UIPopupFactory factory) : base(new Policy(factory))
		{
		}

		void IPopupPool.Release(IPopup popup) => Release((T) popup);

		private class Policy : IObjectPoolPolicy<T>
		{
			private readonly UIPopupFactory _factory;

			public Policy(UIPopupFactory factory)
			{
				_factory = factory;
			}

			public T Create()
			{
				return _factory.Create<T>();
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
