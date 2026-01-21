using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Utility;
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
		public Type type;

		[OnValueChanged(nameof(OnHostChanged))]
		public UIWidgetInspector host;

		public Toggle<RectTransform> customAnchor;

		public UIWidgetArgsInspector argsInspector;

		internal void Show()
		{
			if (type == null)
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
				.MakeGenericMethod(type)
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

			var argsType = UIDispatcherUtility.ResolveArgsType(type);

			if (type == null)
				return;

			argsInspector.SetType(argsType);
		}

		private void OnHostChanged()
		{
			customAnchor = host.Layout;
		}
	}
}
