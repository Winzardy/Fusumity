using System;
using Content;
using Sapientia;
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
		private bool _capacityEnsured;

		public PopupPool(UIPopupFactory factory) : base(new Policy(factory))
		{
		}

		void IPopupPool.Release(IPopup popup) => Release((T) popup);

		public override T Get()
		{
			var popup = base.Get();
			EnsureCapacity(popup.Id);
			return popup;
		}

		// Костыль, достать Entry без Id сложно, в C# 11 появилось static abstract... оно бы это решило
		private void EnsureCapacity(string entryId)
		{
			if (_capacityEnsured)
				return;

			var entry = ContentManager.Get<UIPopupEntry>(entryId);
			if (entry.poolCapacity)
				SetCapacity(entry.poolCapacity);

			_capacityEnsured = true;
		}

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
