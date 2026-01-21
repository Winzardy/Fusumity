using System;
using Sirenix.OdinInspector;
using UI.Editor;

namespace UI.Screens.Editor
{
	public partial class UIDispatcherEditorScreenTab : IUIDispatcherEditorTab
	{
		private UIScreenDispatcher _dispatcher => UIDispatcher.Get<UIScreenDispatcher>();
		int IUIDispatcherEditorTab.Order => 3;

		public string Title => "Screens";
		public SdfIconType? Icon => SdfIconType.Display;

		[OnValueChanged(nameof(OnTypeChanged))]
		public Type type;

		public UIWidgetArgsInspector argsInspector;

		internal void Show()
		{
			if (type == null)
			{
				GUIDebug.LogError("Выберите тип экрана!");
				return;
			}

			_dispatcher?.GetType()
				.GetMethod(nameof(_dispatcher.Show))?
				.MakeGenericMethod(type)
				.Invoke(_dispatcher, new object[]
				{
					argsInspector.GetArgs()
				});
		}

		private void OnTypeChanged()
		{
			argsInspector.Clear();

			var argsType = UIDispatcherUtility.ResolveArgsType(type);

			if (type == null)
				return;

			argsInspector.SetType(argsType);
		}
	}
}
