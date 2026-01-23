using System.Collections.Generic;
using System.Linq;
using Sapientia.Collections;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace UI
{
	public abstract class GroupStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
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
#endif
				item.Switch(state);
			}
		}

		public void DrawListButtonsEditor()
		{
#if UNITY_EDITOR
			if (SirenixEditorGUI.ToolbarButton(new GUIContent("Clear", "Clear from empty (NRE, missing)")))
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
			var children = gameObject.GetComponentsInChildren<StateSwitcher<TState>>(true);

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
			UnityEditor.EditorUtility.SetDirty(this);
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
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}
	}
}
