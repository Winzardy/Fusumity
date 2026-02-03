using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif
using UnityEngine;

namespace UI
{
	public abstract class GroupStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		[NotNull]
		[ListDrawerSettings(OnTitleBarGUI = nameof(DrawListButtonsEditor))]
		protected List<StateSwitcher<TState>> _group;

		protected override void OnStateSwitched(TState state)
		{
			foreach (var item in _group)
			{
#if DebugLog
				if (item == this)
				{
					GUIDebug.LogError("Used same layout in group");
					continue;
				}

				if (item == null)
				{
					GUIDebug.LogError("Used null in group", this);
					continue;
				}
#endif
				item.Switch(state);
			}
		}

		#region Editor

		private bool _useParent;

		public void DrawListButtonsEditor()
		{
#if UNITY_EDITOR
			_useParent = SirenixEditorGUI.ToolbarToggle(
				_useParent,
				new GUIContent("Use Parent", "Использовать родителя при сборе списка")
			);

			if (SirenixEditorGUI.ToolbarButton(new GUIContent("Clear", "Очистить от пустых и null")))
			{
				TryClear();
			}

			if (SirenixEditorGUI.ToolbarButton(SdfIconType.Diagram2Fill))
			{
				AddChildren();
			}

			if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
			{
				TryClear();
				AddChildren();
			}
#endif
		}

		[ContextMenu("Add Children")]
		private void AddChildren()
		{
			var anchor = _useParent ? gameObject.transform.parent : gameObject.transform;
			var children = anchor.GetComponentsInChildren<StateSwitcher<TState>>(true);

			foreach (var switcher in children)
			{
				if (switcher == this)
					continue;

				if (!_group.Contains(switcher))
				{
					_group.Add(switcher);
				}
			}

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		private void TryClear()
		{
			using (ListPool<StateSwitcher<TState>>.Get(out var list))
			{
				for (int i = 0; i < _group.Count; i++)
				{
					if (_group[i] != null)
						list.Add(_group[i]);
				}

				_group = list.ToList();
			}

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		#endregion
	}
}
