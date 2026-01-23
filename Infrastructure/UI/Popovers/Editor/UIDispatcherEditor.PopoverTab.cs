using System.Linq;
using System.Reflection;
using Sapientia;
using Sirenix.OdinInspector;
using UI.Editor;
using UnityEngine;

namespace UI.Popovers.Editor
{
	public partial class UIDispatcherEditorPopoverTab : IUIDispatcherEditorTab
	{
		private UIPopoverDispatcher _dispatcher => UIDispatcher.Get<UIPopoverDispatcher>();

		public string Title => "Popovers";
		public SdfIconType? Icon => SdfIconType.ChatSquareText;

		[OnValueChanged(nameof(OnTypeChanged))]
		[SerializeReference]
		public IPopover popover;

		[OnValueChanged(nameof(OnHostChanged))]
		public UIWidgetInspector host;

		public Toggle<RectTransform> customAnchor;

		public UIWidgetArgsInspector argsInspector;

		internal void Show()
		{
			if (popover == null)
			{
				GUIDebug.LogError("Выберите тип поповера!");
				return;
			}

			if (host.IsEmpty)
			{
				GUIDebug.LogError("Выберите хоста!");
				return;
			}

			RectTransform anchor = customAnchor ? customAnchor : null;
			_dispatcher?.GetType()
				.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.First(m =>
					m.Name == nameof(UIPopoverDispatcher.Show) &&
					m.IsGenericMethodDefinition &&
					m.GetGenericArguments().Length == 1 &&
					m.GetParameters().Length == 3)
				.MakeGenericMethod(popover.GetType())
				.Invoke(_dispatcher, new[]
				{
					host.widget,
					argsInspector.GetArgs(),
					anchor
				});
		}

		private void OnTypeChanged()
		{
			argsInspector.Clear();

			if (popover == null)
				return;

			argsInspector.SetType(popover.GetArgsType());
		}

		private void OnHostChanged()
		{
			customAnchor = host.Layout;
		}
	}
}
