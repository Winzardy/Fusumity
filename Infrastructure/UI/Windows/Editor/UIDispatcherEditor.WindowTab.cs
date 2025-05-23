using System;
using System.Collections;
using Fusumity.Utility;
using Sapientia.Reflection;
using Sapientia.ServiceManagement;
using Sirenix.OdinInspector;
using UI.Editor;

namespace UI.Windows.Editor
{
	public class UIDispatcherEditorWindowTab : IUIDispatcherEditorTab
	{
		private UIWindowDispatcher _dispatcher => ServiceLocator<UIWindowDispatcher>.Instance;
		int IUIDispatcherEditorTab.Order => 0;

		public string Title => "Windows";

		[ShowInInspector, HideLabel, InlineButton(nameof(TryShowWindowEditor), " Show ")]
		[ValueDropdown(nameof(GetWindowTypes))]
		[OnValueChanged(nameof(WindowTypeChanged))]
		public Type windowType;

		[PolymorphicDrawerSettings(ReadOnlyIfNotNullReference = true)]
		[ShowInInspector, ShowIf(nameof(windowArgs), null), LabelText("Args")]
		public IWindowArgs windowArgs;

		private void WindowTypeChanged()
		{
			windowArgs = null;

			var baseType = windowType?.BaseType;

			if (baseType is not {IsGenericType: true})
				return;

			var arguments = baseType.GetGenericArguments();

			if (arguments.Length < 2)
				return;

			var type = arguments[1];

			if (type == typeof(EmptyWindowArgs))
				return;

			windowArgs = type.CreateInstance<IWindowArgs>();
		}

		private void TryShowWindowEditor()
		{
			if (windowType == null)
			{
				GUIDebug.LogError("Выберите тип экрана!");
				return;
			}

			_dispatcher?.GetType()
			   .GetMethod(nameof(_dispatcher.Show))?
			   .MakeGenericMethod(windowType)
			   .Invoke(_dispatcher, new object[]
				{
					windowArgs
				});
		}

		[PropertySpace(10, 0)]
		[Button("Hide")]
		private void HideWindowEditor()
		{
			_dispatcher.TryHideCurrent();
		}

		private IEnumerable GetWindowTypes()
		{
			var types = ReflectionUtility.GetAllTypes<IWindow>(false);
			foreach (var type in types)
			{
				var name = type.Name
				   .Replace("Window", string.Empty);

				yield return new ValueDropdownItem(name, type);
			}
		}
	}
}
