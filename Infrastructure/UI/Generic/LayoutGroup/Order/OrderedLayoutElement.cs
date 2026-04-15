using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace UI
{
	[DisallowMultipleComponent]
	public class OrderedLayoutElement : MonoBehaviour, IOrderedLayoutElement
	{
		private int _currentOrder;
		private int _total;

		[Tooltip("Компоненты-реакторы, которые будут вызваны при SetOrder(index)")]
		[ListDrawerSettings(OnTitleBarGUI = nameof(DrawListButtonsEditor))]
		[SerializeField]
		private OrderedLayoutElementReactor[] _reactors;

		[ShowInInspector] public int CurrentOrder { get => _currentOrder; set => SetOrder(value, _total); }

		public void SetOrder(int index, int total)
		{
			_currentOrder = index;
			_total        = total;

			if (_reactors == null)
				return;

			for (int i = 0; i < _reactors.Length; i++)
			{
				var reactor = _reactors[i];
				if (reactor)
					reactor.OnOrderChanged(index);
			}
		}

		[ContextMenu("Add Reactors")]
		private void AddReactors()
		{
			var reactors = GetComponentsInChildren<OrderedLayoutElementReactor>(true);
			if (reactors == null)
				return;

			for (int i = 0; i < reactors.Length; i++)
			{
				var reactor = reactors[i];
				if (!reactor)
					continue;

				if (Contains(reactor))
					continue;

				Add(reactor);
			}

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		[ContextMenu("Clear Reactors")]
		private void TryClear()
		{
			if (_reactors == null || _reactors.Length == 0)
				return;

			var list = new System.Collections.Generic.List<OrderedLayoutElementReactor>(_reactors.Length);
			for (int i = 0; i < _reactors.Length; i++)
			{
				var reactor = _reactors[i];
				if (!reactor)
					continue;

				if (!list.Contains(reactor))
					list.Add(reactor);
			}

			_reactors = list.ToArray();

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		[ContextMenu("Refresh Reactors")]
		private void RefreshReactors()
		{
			TryClear();
			AddReactors();
		}

		public void DrawListButtonsEditor()
		{
#if UNITY_EDITOR
			if (SirenixEditorGUI.ToolbarButton(new GUIContent("Clear", "Очистить от null и дублей")))
				TryClear();

			if (SirenixEditorGUI.ToolbarButton(SdfIconType.Diagram2Fill))
				AddReactors();

			if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
				RefreshReactors();
#endif
		}

		private bool Contains(OrderedLayoutElementReactor reactor)
		{
			if (_reactors == null)
				return false;

			for (int i = 0; i < _reactors.Length; i++)
			{
				if (_reactors[i] == reactor)
					return true;
			}

			return false;
		}

		private void Add(OrderedLayoutElementReactor reactor)
		{
			if (_reactors == null || _reactors.Length == 0)
			{
				_reactors = new[] {reactor};
				return;
			}

			var temp = new OrderedLayoutElementReactor[_reactors.Length + 1];
			for (int i = 0; i < _reactors.Length; i++)
				temp[i] = _reactors[i];
			temp[temp.Length - 1] = reactor;
			_reactors             = temp;
		}
	}
}
