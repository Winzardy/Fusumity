using Fusumity.Utility;
using System;

namespace Fusumity.MVVM
{
	public class ClickableSubscription : ActionSubscription
	{
		private IClickable _clickable;

		public ClickableSubscription(IClickable clickable, Action action) : base(action)
		{
			_clickable = clickable;
			_clickable.Clicked += Invoke;
		}

		protected override void OnDispose()
		{
			_clickable.Clicked -= Invoke;
		}
	}

	public class ClickableSubscription<T> : ActionSubscription<T>
	{
		private IClickable<T> _clickable;

		public ClickableSubscription(IClickable<T> clickable, Action<T> handler) : base(handler)
		{
			_clickable = clickable;
			_clickable.Clicked += Invoke;
		}

		protected override void OnDispose()
		{
			_clickable.Clicked -= Invoke;
		}
	}
}
