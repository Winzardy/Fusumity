using ActionBusSystem;
using System;
using UnityEngine.UI;

namespace UI
{
	public static class UIButtonExtensions
	{
		public static UnityButtonBusElement Subscribe(this Button button, Action action, string uId = null, string groupId = null, bool allowLinking = true)
		{
			var element = new UnityButtonBusElement(button, action, uId, groupId);
			element.AllowLinking = allowLinking;
			return ActionBus.Register(element);
		}

		public static ActionBusElement Subscribe(this UIButtonLayout layout, Action action)
		{
			return Subscribe(layout.button, action, layout.uId, layout.groupId);
		}

		public static ActionBusElement Subscribe(this UILegacyLabeledButtonLayout layout, Action action)
		{
			return Subscribe(layout.button, action, layout.uId, layout.groupId);
		}

		public static ActionBusElement Subscribe(this UIStatefulButtonLayout layout, Action action)
		{
			return Subscribe(layout.button, action, layout.uId, layout.groupId);
		}

		public static ActionBusElement Subscribe(this ActionBusButtonScheme scheme, Action action)
		{
			return Subscribe(scheme.button, action, scheme.uId, scheme.groupId);
		}

		public static void Unsubscribe(this UIButtonLayout button, Action action)
		{
			Unsubscribe(button.button, action);
		}

		public static void Unsubscribe(this UILegacyLabeledButtonLayout button, Action action)
		{
			Unsubscribe(button.button, action);
		}

		public static void Unsubscribe(this Button button, Action action)
		{
			if (button != null)
			{
				//Снимаем именно подписку с этим action, а не все бас-элементы GameObject
				ActionBus.Unregister(button.gameObject, action);
			}
		}

		public static void Unsubscribe(this ActionBusButtonScheme scheme, Action action)
		{
			Unsubscribe(scheme.button, action);
		}
	}
}
