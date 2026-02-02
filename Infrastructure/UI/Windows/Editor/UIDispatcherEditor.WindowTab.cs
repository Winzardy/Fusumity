using System;
using Sapientia;
using Sapientia.Reflection;
using Sirenix.OdinInspector;
using UI.Editor;
using UnityEngine;

namespace UI.Windows.Editor
{
	public partial class UIDispatcherEditorWindowTab : IUIDispatcherEditorTab
	{
		private UIWindowDispatcher _dispatcher => UIDispatcher.Get<UIWindowDispatcher>();
		int IUIDispatcherEditorTab.Order => 0;

		public string Title => "Windows";
		public SdfIconType? Icon => SdfIconType.Window;

		[OnValueChanged(nameof(OnTypeChanged))]
		[SerializeReference]
		public IWindow window;

		public UIWidgetArgsInspector argsInspector;

		internal void Show(Toggle<WindowMode> mode)
		{
			if (window == null)
			{
				GUIDebug.LogError("Выберите тип окна!");
				return;
			}

			WindowMode targetMode = mode ? mode : WindowMode.Default;
			_dispatcher?.GetType()
				.GetMethod(nameof(_dispatcher.Show))?
				.MakeGenericMethod(window.GetType())
				.Invoke(_dispatcher, new []
				{
					argsInspector.GetArgs(),
					targetMode
				});
		}

		private void OnTypeChanged()
		{
			argsInspector.Clear();

			if (window == null)
				return;

			argsInspector.SetType(window.GetArgsType());
		}
	}
}
