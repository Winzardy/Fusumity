using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Utility;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
	[Serializable]
	[InlineProperty]
	public struct UIWidgetInspector
	{
		private const char ZWSP = '\u200B'; // zero width space

		private static MethodInfo _getBaseMethodInfo;
		private static readonly Dictionary<string, int> _pathToMatchCount = new();

		[HideInInspector]
		public UIWidget widget;

		public RectTransform Layout => widget?.RectTransform;

		public bool IsEmpty => widget == null;

		[OnInspectorGUI]
		private void OnInspectorGUI()
		{
			SirenixEditorGUI.BeginIndentedHorizontal();
			{
				var buttonContent = new GUIContent(widget?.RectTransform?.name ?? "None");
				var current = widget;
				var selected = GenericSelector<UIWidget>.DrawSelectorDropdown(
					GUIContent.none,
					buttonContent,
					rect =>
					{
						var selector = new GenericSelector<UIWidget>(
							"Select",
							false,
							CollectAllWidgets());
						selector.SelectionTree.Config.DrawSearchToolbar = true;
						selector.EnableSingleClickToSelect();

						if (current)
							selector.SetSelection(current);

						selector.ShowInPopup(rect);

						return selector;
					});

				if (selected == null)
				{
				}
				else if (!selected.Any())
				{
					widget = null;
				}
				else
				{
					widget = selected.FirstOrDefault();
				}

				GUIHelper.PushGUIEnabled(false);
				{
					EditorGUILayout.ObjectField(Layout, typeof(RectTransform));
				}
				GUIHelper.PopGUIEnabled();
			}
			SirenixEditorGUI.EndIndentedHorizontal();
		}

		private static IEnumerable<GenericSelectorItem<UIWidget>> CollectAllWidgets()
		{
			yield return new GenericSelectorItem<UIWidget>("None", null);
			var allDispatcherTypes = ReflectionUtility.GetAllTypes<IWidgetDispatcher>();

			foreach (var type in allDispatcherTypes)
			{
				_getBaseMethodInfo ??= typeof(UIDispatcher)
					.GetMethods(BindingFlags.Public | BindingFlags.Static)
					.First(m =>
						m.Name == nameof(UIDispatcher.Get) &&
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

		private static IEnumerable<GenericSelectorItem<UIWidget>> EnumerateWithPaths(UIWidget node, string prefix)
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

			yield return new GenericSelectorItem<UIWidget>(path, node);

			if (node.Children != null)
			{
				foreach (var child in node.Children)
				{
					foreach (var item in EnumerateWithPaths(child, path))
						yield return item;
				}
			}
		}

		public static implicit operator UIWidget(UIWidgetInspector inspector) => inspector.widget;
	}
}
