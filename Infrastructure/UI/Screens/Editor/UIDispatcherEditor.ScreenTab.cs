using System;
using Sapientia.Reflection;
using Sirenix.OdinInspector;
using UI.Editor;

namespace UI.Screens.Editor
{
	public class UIDispatcherEditorScreenTab : IUIDispatcherEditorTab
	{
		private UIScreenDispatcher _dispatcher => UIDispatcher.Get<UIScreenDispatcher>();
		int IUIDispatcherEditorTab.Order => 3;

		public string Title => "Screens";

		[OnValueChanged(nameof(OnTypeChanged))]
		public Type type;

		public IScreenArgs args;

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

			var baseType = this.type?.BaseType;

			if (baseType is not {IsGenericType: true})
				return;

			var arguments = baseType.GetGenericArguments();

			if (arguments.Length < 2)
				return;

			var type = arguments[1];

			if (type == typeof(EmptyScreenArgs))
				return;

			args = type.CreateInstance<IScreenArgs>();
		}

		// [Title("Other", "разные системные методы", titleAlignment: TitleAlignments.Split)]
		// [Button("Try Show Default")]
		// [PropertySpace(10)]
		// private void TryShowDefaultScreenEditor()
		// {
		// 	_dispatcher.TryShowDefault(false);
		// }
		//
		// [HorizontalGroup("Blockers")]
		// [Button("Add Show Blocker")]
		// private void AddShowBlockerEditor()
		// {
		// 	_dispatcher.AddShowBlocker(this);
		// }
		//
		// [HorizontalGroup("Blockers")]
		// [Button("Remove Show Blocker")]
		// private void RemoveShowBlockerEditor()
		// {
		// 	_dispatcher.RemoveShowBlocker(this);
		// }

		[Title("Other", "разные системные методы", titleAlignment: TitleAlignments.Split)]
		[PropertySpace(10, 0)]
		[Button("Hide Current")]
		private void HideScreenEditor()
		{
			_dispatcher.TryHideCurrent();
		}
	}
}
