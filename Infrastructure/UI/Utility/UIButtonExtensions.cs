using ActionBusSystem;
using System;
using UnityEngine.UI;

namespace UI
{
	public static class UIButtonExtensions
	{
		public static ActionBusElement Subscribe(this Button button, Action action, string uId = null, string groupId = null, bool allowLinking = true)
		{
			var element = new UnityButtonBusElement(button, action, uId, groupId);
			element.AllowLinking = allowLinking;
			return ActionBus.Register(element);
		}

		public static ActionBusElement Subscribe(this UIButtonLayout layout, Action action)
		{
			return Subscribe(layout.button, action, layout.uId, layout.groupId);
		}

		public static ActionBusElement Subscribe(this UILabeledButtonLayout layout, Action action)
		{
			return Subscribe(layout.button, action, layout.uId, layout.groupId);
		}

		public static ActionBusElement Subscribe(this UIStatefulButtonLayout layout, Action action)
		{
			return Subscribe(layout.button, action, layout.uId, layout.groupId);
		}

		public static void Unsubscribe(this UIButtonLayout button, Action action)
		{
			Unsubscribe(button.button, action);
		}

		public static void Unsubscribe(this UILabeledButtonLayout button, Action action)
		{
			Unsubscribe(button.button, action);
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
