using System;
using Sirenix.OdinInspector;
using UI.Editor;
using UnityEngine;

namespace UI.Screens.Editor
{
	public partial class UIDispatcherEditorScreenTab : IUIDispatcherEditorTab
	{
		private UIScreenDispatcher _dispatcher => UIDispatcher.Get<UIScreenDispatcher>();
		int IUIDispatcherEditorTab.Order => 3;

		public string Title => "Screens";
		public SdfIconType? Icon => SdfIconType.Display;

		[OnValueChanged(nameof(OnTypeChanged))]
		[SerializeReference]
		public IScreen screen;

		public UIWidgetArgsInspector argsInspector;

		internal void Show()
		{
			if (screen == null)
			{
				GUIDebug.LogError("Выберите тип экрана!");
				return;
			}

			var autoClear = screen is IBoundedView {AutoDisposeViewModel: true};
			_dispatcher?.GetType()
				.GetMethod(nameof(_dispatcher.Show))?
				.MakeGenericMethod(screen.GetType())
				.Invoke(_dispatcher, new object[]
				{
					argsInspector.GetArgs(autoClear)
				});
		}

		private void OnTypeChanged()
		{
			argsInspector.Clear();

			if (screen == null)
				return;

			argsInspector.SetType(screen.GetArgsType());
		}
	}
}
