using System;
using UnityEngine;

namespace UI
{
	public abstract partial class UIBaseLayout
	{
		internal event Action<string> SignalReceived;

		public const string SIGNAL_CONTEXT_MENU_GROUP = "Signal/";
		public const string SIGNAL_CONTEXT_MENU_DEFAULT_ITEM_NAME = "Default";
		public const string SIGNAL_CONTEXT_MENU_ITEM_ENTER_NAME = "Enter name";

		[ContextMenu(SIGNAL_CONTEXT_MENU_GROUP + SIGNAL_CONTEXT_MENU_DEFAULT_ITEM_NAME, false, priority: 100000)]
		private void SendDefaultSignal()
		{
			SendSignal(SIGNAL_CONTEXT_MENU_DEFAULT_ITEM_NAME);
		}

#if UNITY_EDITOR
		[ContextMenu(SIGNAL_CONTEXT_MENU_GROUP + SIGNAL_CONTEXT_MENU_ITEM_ENTER_NAME, false, priority: 1000)]
		private void ShowPopupAndSendSignal()
		{
			SignalEnterNamePopup.Open(this, string.Empty);
		}
#endif

		public void SendSignal(string signalName)
		{
			SignalReceived?.Invoke(signalName);
		}
	}
}
