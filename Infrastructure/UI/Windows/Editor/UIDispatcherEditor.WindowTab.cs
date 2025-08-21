using System;
using Sapientia.Reflection;
using Sirenix.OdinInspector;
using UI.Editor;

namespace UI.Windows.Editor
{
	public class UIDispatcherEditorWindowTab : IUIDispatcherEditorTab
	{
		private UIWindowDispatcher _dispatcher => UIDispatcher.Get<UIWindowDispatcher>();
		int IUIDispatcherEditorTab.Order => 0;

		public string Title => "Windows";

		[OnValueChanged(nameof(OnTypeChanged))]
		public Type type;

		public IWindowArgs args;

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

			if (type == typeof(EmptyWindowArgs))
				return;

			args = type.CreateInstance<IWindowArgs>();
		}

		[Title("Other", "разные системные методы", titleAlignment: TitleAlignments.Split)]
		[PropertySpace(10, 0)]
		[Button("Hide Current")]
		private void HideWindowEditor()
		{
			_dispatcher.TryHideCurrent();
		}
	}
}
