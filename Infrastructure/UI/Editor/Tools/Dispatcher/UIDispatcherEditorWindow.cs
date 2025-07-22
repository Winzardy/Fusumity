using System.Collections.Generic;
using Fusumity.Editor;
using Fusumity.Utility;
using Sapientia.Pooling;
using Sapientia.Reflection;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;
using OdinMenuTree = Sirenix.OdinInspector.Editor.OdinMenuTree;

namespace UI.Editor
{
	public class UIDispatcherEditorWindow : OdinMenuEditorWindow
	{
		private List<IUIDispatcherEditorTab> _tabs;

		protected override void OnImGUI()
		{
			if (!Application.isPlaying)
			{
				FusumityEditorGUILayout.DrawWarning("Только в Play Mode", iconSize: 60);
				return;
			}

			base.OnImGUI();
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			titleContent = new GUIContent("UI Dispatcher", EditorIcons.GridLayout.Active);
			minSize = new Vector2(512, 256);
		}

		protected override void Initialize()
		{
			_tabs = ListPool<IUIDispatcherEditorTab>.Get();

			var types = ReflectionUtility.GetAllTypes<IUIDispatcherEditorTab>(editor: true);
			using (ListPool<IUIDispatcherEditorTab>.Get(out var list))
			{
				foreach (var type in types)
					list.Add(type.CreateInstance<IUIDispatcherEditorTab>());

				list.Sort(Comparison);

				_tabs.AddRange(list);
			}

			int Comparison(IUIDispatcherEditorTab x, IUIDispatcherEditorTab y) => x.Order.CompareTo(y.Order);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			_tabs.ReleaseToStaticPool();
		}

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = new OdinMenuTree();

			foreach (var tab in _tabs)
				tree.Add(tab.Title, tab);

			return tree;
		}

		private void OnValidate()
		{
			titleContent = new GUIContent("UI Dispatcher", EditorIcons.GridLayout.Active);
		}
	}
}
