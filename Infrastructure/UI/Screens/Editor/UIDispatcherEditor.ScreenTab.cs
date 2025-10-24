using System;
using System.Collections.Generic;
using Fusumity.Editor.Extensions;
using Sirenix.OdinInspector;
using UI.Editor;

namespace UI.Screens.Editor
{
	public partial class UIDispatcherEditorScreenTab : IUIDispatcherEditorTab
	{
		private Type _argsType;

		private UIScreenDispatcher _dispatcher => UIDispatcher.Get<UIScreenDispatcher>();
		int IUIDispatcherEditorTab.Order => 3;

		public string Title => "Screens";

		[OnValueChanged(nameof(OnTypeChanged))]
		public Type type;

		[TypeFilter(nameof(Args))]
		public object args;

		public bool ArgsVisible => _argsType != null;

		private IEnumerable<Type> Args()
		{
			if (_argsType == null)
				yield break;

			foreach (var type in _argsType.GetInheritorTypes())
				yield return type;
		}

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
					args
				});
		}

		private void OnTypeChanged()
		{
			args = null;
			_argsType = null;

			var baseType = this.type?.BaseType;

			if (baseType is not {IsGenericType: true})
				return;

			var arguments = baseType.GetGenericArguments();

			if (arguments.Length < 2)
				return;

			var argsType = arguments[1];

			if (argsType == typeof(EmptyArgs))
				return;

			_argsType = argsType;
		}
	}
}
