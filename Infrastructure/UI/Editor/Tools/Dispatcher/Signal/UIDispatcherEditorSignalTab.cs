using System.Linq;
using System.Reflection;
using Sapientia.Extensions;
using Sirenix.OdinInspector;

namespace UI.Editor.Signal
{
	public class UIDispatcherEditorSignalTab : IUIDispatcherEditorTab
	{
		public string Title => "Signal";
		public int Order => -100;
		public SdfIconType? Icon => SdfIconType.Wifi;

		public UIWidgetInspector inspector;

		public string signalName;

		internal void Send()
		{
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

		internal bool CanSend => !signalName.IsNullOrEmpty() && !inspector.IsEmpty;
	}
}
