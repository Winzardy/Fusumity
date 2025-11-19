using System.Linq;
using System.Reflection;
using Messaging;
using Sapientia.Extensions;
using Sapientia.Utility;
using Sirenix.OdinInspector;

namespace UI.Editor.Signal
{
	public class UIDispatcherEditorSignalTab : IUIDispatcherEditorTab
	{
		public string Title => "Signal";
		public int Order => -100;
		public SdfIconType? Icon => SdfIconType.Wifi;

		public string signalName;
		public UIWidgetInspector inspector;

		internal void Send()
		{
			if (inspector.IsEmpty)
			{
				new SignalMessage(signalName)
					.Send();
				return;
			}

			var type = inspector.widget?.GetType();
			type?.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.First(m =>
					m.Name == nameof(UIWidget.Signal) &&
					m.GetParameters().Length == 1)
				.Invoke(inspector.widget, new[]
				{
					signalName
				});
		}

		internal bool CanSend => !signalName.IsNullOrEmpty();
	}
}
