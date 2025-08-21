using System;
using Sirenix.OdinInspector;
using UI.Editor;

namespace UI.Screens.Editor
{
	public class UIDispatcherEditorScreenTab : IUIDispatcherEditorTab
	{
		private UIScreenDispatcher _dispatcher => UIDispatcher.Get<UIScreenDispatcher>();
		public string Title => "Screens";

		public int Order => 3;

		public Type type;

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
			   .Invoke(_dispatcher, null);
		}

		[Title("Other","разные системные методы", titleAlignment: TitleAlignments.Split)]
		[Button("Try Show Default")]
		[PropertySpace(10)]
		private void TryShowDefaultScreenEditor()
		{
			_dispatcher.TryShowDefault(false);
		}

		[HorizontalGroup("Blockers")]
		[Button("Add Show Blocker")]
		private void AddShowBlockerEditor()
		{
			_dispatcher.AddShowBlocker(this);
		}

		[HorizontalGroup("Blockers")]
		[Button("Remove Show Blocker")]
		private void RemoveShowBlockerEditor()
		{
			_dispatcher.RemoveShowBlocker(this);
		}

		[Button("Hide Current")]
		private void HideScreenEditor()
		{
			_dispatcher.Hide();
		}
	}
}
