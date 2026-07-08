using System;
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
		private static readonly Type[] _showWithHostParameterTypes =
		{
			typeof(UIWidget),
			typeof(object),
			typeof(RectTransform),
			typeof(bool)
		};

		private static MethodInfo _showWithHostMethod;

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
			_showWithHostMethod ??= typeof(UIPopoverDispatcher)
				.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.First(m => IsShowWithHostMethod(m));

			_showWithHostMethod
				.MakeGenericMethod(popover.GetType())
				.Invoke(_dispatcher, new object[]
				{
					host.widget,
					argsInspector.GetArgs(),
					anchor,
					false
				});
		}

		private static bool IsShowWithHostMethod(MethodInfo method)
		{
			if (method.Name != nameof(UIPopoverDispatcher.Show) ||
			    !method.IsGenericMethodDefinition ||
			    method.GetGenericArguments().Length != 1)
			{
				return false;
			}

			var parameters = method.GetParameters();
			if (parameters.Length != _showWithHostParameterTypes.Length)
				return false;

			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i].ParameterType != _showWithHostParameterTypes[i])
					return false;
			}

			return true;
		}

		private void OnTypeChanged()
		{
			argsInspector.Clear();

			if (popover == null)
				return;

			argsInspector.SetType(popover.GetDeclaredArgsType());
		}

		private void OnHostChanged()
		{
			customAnchor = host.Layout;
		}
	}
}
