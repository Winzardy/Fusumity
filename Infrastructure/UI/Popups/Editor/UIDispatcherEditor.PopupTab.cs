using System;
using Sapientia;
using Sirenix.OdinInspector;
using UI.Editor;
using UnityEngine;

namespace UI.Popups.Editor
{
	public partial class UIDispatcherEditorPopupTab : IUIDispatcherEditorTab
	{
		private UIPopupDispatcher _dispatcher => UIDispatcher.Get<UIPopupDispatcher>();
		int IUIDispatcherEditorTab.Order => 2;
		public string Title => "Popups";
		public SdfIconType? Icon => SdfIconType.ChatRightDots;

		[OnValueChanged(nameof(OnTypeChanged))]
		[SerializeReference]
		public IPopup popup;

		public UIWidgetArgsInspector argsInspector;

		internal void Show(Toggle<PopupMode> mode)
		{
			if (popup == null)
			{
				GUIDebug.LogError("Выберите тип попапа!");
				return;
			}

			_dispatcher?.GetType()
				.GetMethod(nameof(_dispatcher.Show))?
				.MakeGenericMethod(popup.GetType())
				.Invoke(_dispatcher, new[]
				{
					argsInspector.GetArgs(),
					mode ? new PopupMode?(mode.value) : null
				});
		}

		private void OnTypeChanged()
		{
			argsInspector.Clear();

			if (popup == null)
				return;

			argsInspector.SetType(popup.GetArgsType());
		}
	}
}
