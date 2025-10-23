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

		[OnValueChanged(nameof(OnTypeChanged))]
		public Type type;

		public object args;

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
					args
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
