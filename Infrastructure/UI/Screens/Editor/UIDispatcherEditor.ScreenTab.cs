using System;
using System.Collections;
using Fusumity.Utility;
using Sapientia.ServiceManagement;
using Sirenix.OdinInspector;
using UI.Editor;

namespace UI.Screens.Editor
{
	public class UIDispatcherEditorScreenTab : IUIDispatcherEditorTab
	{
		private UIScreenDispatcher _dispatcher => ServiceLocator<UIScreenDispatcher>.Instance;
		public string Title => "Screens";

		[ShowInInspector, HideLabel, InlineButton(nameof(TryShowScreenEditor), " Show ")]
		[ValueDropdown(nameof(GetScreenTypes))]
		public Type screenType;

		private void TryShowScreenEditor()
		{
			if (screenType == null)
			{
				GUIDebug.LogError("Выберите тип экрана!");
				return;
			}

			_dispatcher?.GetType()
			   .GetMethod(nameof(_dispatcher.Show))?
			   .MakeGenericMethod(screenType)
			   .Invoke(_dispatcher, null);
		}

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

		[Button("Hide")]
		private void HideScreenEditor()
		{
			_dispatcher.Hide();
		}

		private IEnumerable GetScreenTypes()
		{
			var types = ReflectionUtility.GetAllTypes<IScreen>(false);
			foreach (var type in types)
			{
				var name = type.Name
				   .Replace("Screen", string.Empty);

				yield return new ValueDropdownItem(name, type);
			}
		}
	}
}
