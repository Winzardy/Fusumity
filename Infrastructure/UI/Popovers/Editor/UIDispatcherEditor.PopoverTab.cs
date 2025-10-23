using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Reflection;
using Sirenix.OdinInspector;
using UI.Editor;
using UI.Popovers;
using UnityEngine;

namespace UI.Popups.Editor
{
	public class UIDispatcherEditorPopoverTab : IUIDispatcherEditorTab
	{
		private UIPopoverDispatcher _dispatcher => UIDispatcher.Get<UIPopoverDispatcher>();

		public string Title => "Popovers";

		[OnValueChanged(nameof(OnTypeChanged))]
		public Type type;

		[OnValueChanged(nameof(OnHostChanged))]
		public HostEntry hostEntry;

		public Toggle<RectTransform> customAnchor;

		public IPopoverArgs args;

		internal void Show()
		{
			if (type == null)
			{
				GUIDebug.LogError("Выберите тип поповера!");
				return;
			}

			if (hostEntry.IsEmpty)
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
			   .Invoke(_dispatcher, new object[]
				{
					hostEntry.widget,
					args,
					anchor
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

			if (type == typeof(EmptyPopoverArgs))
				return;

			args = type.CreateInstance<IPopoverArgs>();
		}

		private void OnHostChanged()
		{
			customAnchor = hostEntry.widget.RectTransform;
		}

		[Title("Other", "разные системные методы", titleAlignment: TitleAlignments.Split)]
		[PropertySpace(10, 0)]
		[Button("Hide Last")]
		private void HideLastEditor()
		{
			_dispatcher.TryHideLast();
		}

		[Serializable]
		[InlineProperty]
		public struct HostEntry
		{
			private const char ZWSP = '\u200B'; // zero width space

			private static MethodInfo _getBaseMethodInfo;

			private static Dictionary<string, int> _pathToMatchCount = new();

			[ShowInInspector, HideLabel, HorizontalGroup]
			[ValueDropdown("@" + nameof(HostEntry) + "." + nameof(CollectAllWidgets) + "()")]
			public UIWidget widget;

			[ShowInInspector, HideLabel, HorizontalGroup(0.35f)]
			public RectTransform Layout => widget?.RectTransform;

			public bool IsEmpty => widget == null;
			public static implicit operator UIWidget(HostEntry entry) => entry.widget;

			private static IEnumerable<ValueDropdownItem<UIWidget>> CollectAllWidgets()
			{
				var allDispatcherTypes = ReflectionUtility.GetAllTypes<IWidgetDispatcher>();

				foreach (var type in allDispatcherTypes)
				{
					_getBaseMethodInfo ??= typeof(UIDispatcher)
					   .GetMethods(BindingFlags.Public | BindingFlags.Static)
					   .First(m =>
							m.Name == nameof(UIDispatcher.GetLayer) &&
							m.IsGenericMethodDefinition &&
							m.GetGenericArguments().Length == 1 &&
							m.GetParameters().Length == 0);

					if (_getBaseMethodInfo == null)
						continue;

					var totalMethodInfo = _getBaseMethodInfo
					   .MakeGenericMethod(type);

					if (totalMethodInfo.Invoke(null, null) is not IWidgetDispatcher dispatcher)
						continue;

					_pathToMatchCount.Clear();

					foreach (var root in dispatcher.GetAllActive())
					{
						foreach (var item in EnumerateWithPaths(root, null))
							yield return item;
					}
				}
			}

			private static IEnumerable<ValueDropdownItem<UIWidget>> EnumerateWithPaths(UIWidget node, string prefix)
			{
				if (node == null || !node.RectTransform)
					yield break;

				var postfix = node.Active
					? ""
					: " (inactive)";
				var name = $"{node.RectTransform.name.Trim()}{postfix}";
				var path = string.IsNullOrEmpty(prefix) ? name : $"{prefix}/{name}";

				if (!_pathToMatchCount.TryAdd(path, 0))
				{
					_pathToMatchCount[path]++;
					path += new string(ZWSP, _pathToMatchCount[path]);
				}

				yield return new ValueDropdownItem<UIWidget>(path, node);

				if (node.Children != null)
				{
					foreach (var child in node.Children)
					{
						foreach (var item in EnumerateWithPaths(child, path))
							yield return item;
					}
				}
			}
		}
	}
}
