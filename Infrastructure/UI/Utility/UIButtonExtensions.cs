using ActionBusSystem;
using System;
using UnityEngine.UI;

namespace UI
{
	public static class UIButtonExtensions
	{
		public static ActionBusElement Subscribe(this Button button, Action action, string uId = null, string groupId = null)
		{
			var subscription = new UnityButtonBusElement(button, action, uId, groupId);
			return ActionBus.Register(subscription);
		}

		public static ActionBusElement Subscribe(UILabeledButtonLayout layout, Action action)
		{
			return Subscribe(layout.button, action, layout.uId, layout.groupId);
		}

		public static ActionBusElement Subscribe(UIStatefulButtonLayout layout, Action action)
		{
			return Subscribe(layout.button, action, layout.uId, layout.groupId);
		}

		public static void Unsubscribe(this Button button, Action action)
		{
			if (button != null)
			{
				ActionBus.Unregister(button.gameObject);
			}
		}
	}
}
