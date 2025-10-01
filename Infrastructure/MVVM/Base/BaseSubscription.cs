using System;
using UnityEngine.Assertions;

namespace Fusumity.MVVM
{
	public abstract class BaseSubscription<TDelegate> : IDisposable where TDelegate : Delegate
	{
		protected TDelegate _delegate;

		public bool IsDisposed { get; private set; }

		public BaseSubscription(TDelegate @delegate)
		{
			_delegate = @delegate;
		}

		public void Dispose()
		{
			if (IsDisposed)
				return;

			OnDispose();
			IsDisposed = true;

			_delegate = null;
		}

		protected abstract void OnDispose();
	}

	public abstract class ActionSubscription : BaseSubscription<Action>
	{
		public event Action Invoked;

		protected ActionSubscription(Action action) : base(action)
		{
			Assert.IsNotNull(action, "action cannot be null");
		}

		public void Invoke()
		{
			_delegate.Invoke();
			Invoked?.Invoke();
		}
	}

	public abstract class FuncSubscription<TResult> : BaseSubscription<Func<TResult>>
	{
		public event Action<TResult> Invoked;

		protected FuncSubscription(Func<TResult> func) : base(func)
		{
			Assert.IsNotNull(func, "func cannot be null");
		}

		public void Invoke()
		{
			var result = _delegate.Invoke();
			Invoked?.Invoke(result);
		}
	}

	public abstract class FuncSubscription<TInput, TResult> : BaseSubscription<Func<TInput, TResult>>
	{
		public event Action<TResult> Invoked;

		protected FuncSubscription(Func<TInput, TResult> func) : base(func)
		{
			Assert.IsNotNull(func, "func cannot be null");
		}

		public void Invoke(TInput param)
		{
			var result = _delegate.Invoke(param);
			Invoked?.Invoke(result);
		}
	}
}
