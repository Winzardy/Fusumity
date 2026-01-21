using System;
using Sapientia.Reflection;
using Sirenix.OdinInspector;
using UI.Editor;

namespace UI.Windows.Editor
{
	public partial class UIDispatcherEditorWindowTab : IUIDispatcherEditorTab
	{
		private UIWindowDispatcher _dispatcher => UIDispatcher.Get<UIWindowDispatcher>();
		int IUIDispatcherEditorTab.Order => 0;

		public string Title => "Windows";
		public SdfIconType? Icon => SdfIconType.Window;

		[OnValueChanged(nameof(OnTypeChanged))]
		public Type type;

		public UIWidgetArgsInspector argsInspector;

		internal void Show()
		{
			if (type == null)
			{
				GUIDebug.LogError("Выберите тип окна!");
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
