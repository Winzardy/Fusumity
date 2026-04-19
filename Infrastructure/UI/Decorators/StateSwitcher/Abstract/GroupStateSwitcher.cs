using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sapientia.Collections;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace UI
{
	public interface IGroupStateSwitcher : IStateSwitcher
	{
		IEnumerable<IStateSwitcher> Children { get; }
	}

	public abstract class GroupStateSwitcher<TState> : StateSwitcher<TState>, IGroupStateSwitcher, ISerializationCallbackReceiver
	{
		[NotNull]
		[ListDrawerSettings(OnTitleBarGUI = nameof(DrawListButtonsEditor), CustomRemoveElementFunction = nameof(OnRemoveGroupElement))]
		[OnValueChanged(nameof(RefreshParentLinks))]
		[SerializeField]
		protected List<StateSwitcher<TState>> _group;

		public IEnumerable<IStateSwitcher> Children => _group;

		protected virtual void Awake() => RefreshParentLinks();

		protected override void OnStateSwitched(TState state)
		{
			foreach (var item in _group)
			{
#if DebugLog
				if (item == this)
				{
					Debug.LogError("Used same layout in group");
					continue;
				}

				if (item == null)
				{
					Debug.LogError("Used null in group", this);
					continue;
				}
#endif
				item.Switch(state);
			}
		}

		private void RefreshParentLinks()
		{
			if (_group.IsNullOrEmpty())
				return;

			using (HashSetPool<StateSwitcher<TState>>.Get(out var hashSet))
			using (ListPool<int>.Get(out var removingIndexes))
			{
				foreach (var (child, index) in _group.WithIndex())
				{
					if (child == null)
						continue;

					if (!hashSet.Add(child))
					{
						removingIndexes.Add(index);
						Debug.LogWarning("Duplicate child StateSwitcher detected in group. Duplicate item removed", this);
						continue;
					}

					child.SetParent(this);
				}

				for (var i = removingIndexes.Count - 1; i >= 0; i--)
					_group.RemoveAt(removingIndexes[i]);
			}
		}

		public override bool IsTransitioning()
		{
			foreach (var state in _group)
				if (state.IsTransitioning())
					return true;

			return false;
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

			RefreshParentLinks();

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

			RefreshParentLinks();

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		private void OnRemoveGroupElement(StateSwitcher<TState> child)
		{
			if (child != null)
				child.ClearParent(this);

			_group.Remove(child);
			RefreshParentLinks();
		}
#if UNITY_EDITOR
		private bool _requestedValidate;

		protected virtual void OnValidate()
		{
			if (Application.isPlaying)
				return;

			if (_requestedValidate)
				return;

			_requestedValidate = true;
			EditorApplication.delayCall += () =>
			{
				_requestedValidate = false;
				if (this == null)
					return;

				RefreshParentLinks();
			};
		}
#endif

		#endregion

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize() => RefreshParentLinks();
	}
}
