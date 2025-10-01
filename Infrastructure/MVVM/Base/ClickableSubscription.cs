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
}
