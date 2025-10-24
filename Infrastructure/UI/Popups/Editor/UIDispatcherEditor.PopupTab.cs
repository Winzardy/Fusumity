using System;
using Sirenix.OdinInspector;
using UI.Editor;

namespace UI.Popups.Editor
{
	public partial class UIDispatcherEditorPopupTab : IUIDispatcherEditorTab
	{
		private UIPopupDispatcher _dispatcher => UIDispatcher.Get<UIPopupDispatcher>();
		int IUIDispatcherEditorTab.Order => 2;
		public string Title => "Popups";

		[OnValueChanged(nameof(OnTypeChanged))]
		public Type type;

		public UIWidgetArgsInspector argsInspector;

		internal void Show(bool force = false)
		{
			if (type == null)
			{
				GUIDebug.LogError("Выберите тип попапа!");
				return;
			}

			_dispatcher?.GetType()
				.GetMethod(nameof(_dispatcher.Show))?
				.MakeGenericMethod(type)
				.Invoke(_dispatcher, new[]
				{
					argsInspector.GetArgs(),
					force
				});
		}

		private void OnTypeChanged()
		{
			argsInspector.Clear();

			var baseType = this.type?.BaseType;

			if (baseType is not {IsGenericType: true})
				return;

			var arguments = baseType.GetGenericArguments();

			if (arguments.Length < 2)
				return;

			var argsType = arguments[1];

			if (argsType == typeof(EmptyArgs))
				return;

			argsInspector.SetType(argsType);
		}
	}
}
