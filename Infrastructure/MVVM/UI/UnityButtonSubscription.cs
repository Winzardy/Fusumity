using System;
using UI;
using UnityEngine.UI;

namespace Fusumity.MVVM.UI
{
	public class UnityButtonSubscription : ActionSubscription
	{
		private Button _button;

		public UnityButtonSubscription(Button button, Action action) : base(action)
		{
			_button = button;
			_button.Subscribe(Invoke);
		}

		protected override void OnDispose()
		{
			_button.Unsubscribe(Invoke);
		}
	}
}
