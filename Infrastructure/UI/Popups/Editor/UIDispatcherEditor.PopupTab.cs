using System;
using Sapientia.Reflection;
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

		public object args;

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
				.Invoke(_dispatcher, new object[]
				{
					args,
					force
				});
		}

		private void OnTypeChanged()
		{
			args = null;

			var baseType = this.type?.BaseType;

			if (baseType is not {IsGenericType: true})
				return;

			var arguments = baseType.GetGenericArguments();

			if (arguments.Length < 2)
				return;

			var type = arguments[1];

			if (type == typeof(EmptyArgs))
				return;

			args = type.CreateInstance<object>();
		}
	}
}
