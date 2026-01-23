using System;
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

		internal void Show()
		{
			if (window == null)
			{
				GUIDebug.LogError("Выберите тип окна!");
				return;
			}

			_dispatcher?.GetType()
				.GetMethod(nameof(_dispatcher.Show))?
				.MakeGenericMethod(window.GetType())
				.Invoke(_dispatcher, new object[]
				{
					argsInspector.GetArgs()
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
