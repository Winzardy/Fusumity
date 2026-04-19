using System;
using System.Collections.Generic;
using System.Linq;
using Fusumity.Editor;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace UI.Editor
{
	[Overlay(typeof(SceneView), "State Switcher", "State Switcher")]
	public class UIStateSwitcherEditorOverlay : IMGUIOverlay, ITransientOverlay
	{
		private const float PING_LABEL_LEFT_PADDING = 18f;
		private const float MIN_OVERLAY_WIDTH = 255f;
		private const string NONE = "None";

		private readonly List<List<IStateSwitcher>> _targets = new();
		private readonly Dictionary<IStateSwitcher, Dropdown> _switcherToDropdown = new();
		private bool _isExpanded = true;

		public bool visible => !Application.isPlaying && _targets.Count > 0;

		public override void OnCreated()
		{
			Selection.selectionChanged += OnSelectionChanged;
			OnSelectionChanged();
		}

		public override void OnWillBeDestroyed()
		{
			Selection.selectionChanged -= OnSelectionChanged;
			ClearTargets();
		}

		private void OnSelectionChanged()
		{
			if (Selection.activeGameObject == null)
			{
				ClearTargets();
				return;
			}

			ClearTargets();

			var leafTarget = FindLeafTarget(Selection.activeGameObject.transform);
			if (leafTarget == null)
				return;

			var list = ListPool<IStateSwitcher>.Get();
			_targets.Add(list);
			list.Add(leafTarget);
			var parents = FindParentTargets(leafTarget);
			list.AddRange(parents);

			var last = list[^1];
			var transformParent = last.gameObject.transform.parent;
			if (transformParent == null)
				return;

			for (int i = 0; i < transformParent.childCount; i++)
			{
				var child = transformParent.GetChild(i);
				if (child == last.gameObject.transform)
					continue;

				if (!child.TryGetComponent(out IGroupStateSwitcher groupStateSwitcher) || groupStateSwitcher.Parent != null)
					continue;

				list = ListPool<IStateSwitcher>.Get();
				list.Add(groupStateSwitcher);
				_targets.Add(list);
			}

			_isExpanded = true;
		}

		private void ClearTargets()
		{
			foreach (var dropdownField in _switcherToDropdown.Values)
				dropdownField.Reset();
			_switcherToDropdown.Clear();

			foreach (var list in _targets)
				list.ReleaseToStaticPool();
			_targets.Clear();
		}

		public override void OnGUI()
		{
			if (Application.isPlaying)
				return;

			if (_targets.Count == 0)
				return;

			GUILayoutUtility.GetRect(MIN_OVERLAY_WIDTH, 0f, GUILayout.ExpandWidth(true));

			var useSeparator = false;
			foreach (var group in _targets)
			{
				if (!useSeparator)
					useSeparator = true;
				else
					GUILayout.Label("──", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(4));

				var depth = _isExpanded ? group.Count - 1 : 0;
				var current = group[depth];
				if (group.Count <= 1)
				{
					Header();
					continue;
				}

				FusumityEditorGUILayout.FoldoutContainer(Header, Body, ref _isExpanded, current);

				Rect Header() => DrawSwitcher(current, depth, group.Count);

				void Body()
				{
					for (int i = group.Count - 2; i >= 0; i--)
					{
						var target = group[i];
						if (target == null)
							continue;
						DrawSwitcher(target, i, group.Count);
					}
				}
			}
		}

		private Rect DrawSwitcher(IStateSwitcher target, int depth, int maxDepth)
		{
			var name = target.Name;

			var isSelected = depth == 0 && maxDepth > 1 && _isExpanded;
			if (_isExpanded)
			{
				var count = maxDepth - 1 - depth;

				if (count > 0)
				{
					var prefix = $"└{new string('─', count - 1)} ";
					name = prefix + name;
				}
			}

			{
				switch (target)
				{
					case StateSwitcher<string> switcher:
						return DrawStringSwitcher(switcher, name, isSelected);

					case StateSwitcher<int> switcher:
						return DrawIntSwitcher(switcher, name, isSelected);

					case StateSwitcher<bool> switcher:
						return DrawBoolSwitcher(switcher, name, isSelected);
				}
			}

			return default;
		}

		private Rect DrawStringSwitcher(StateSwitcher<string> switcher, string label, bool forceSelected = false)
		{
			using (ListPool<string>.Get(out var variants))
			using (HashSetPool<object>.Get(out var visited))
			{
				variants.AddRange(CollectStringVariants(switcher, visited));
				var dropdownField = GetStringDropdownField(switcher);
				EditorGUI.BeginChangeCheck();
				var value = dropdownField.Draw(
					variants,
					switcher.Current ?? string.Empty,
					label,
					out var rect,
					forceSelected);
				if (EditorGUI.EndChangeCheck())
					ApplyState(switcher, value);

				HandleLabelPing(switcher, rect);
				return rect;
			}
		}

		private Rect DrawIntSwitcher(StateSwitcher<int> switcher, string label, bool forceSelected)
		{
			EditorGUI.BeginChangeCheck();
			var value = DrawIntField(switcher.Current, label, out var rect, forceSelected);
			if (EditorGUI.EndChangeCheck())
				ApplyState(switcher, value);

			HandleLabelPing(switcher, rect);
			return rect;
		}

		private Rect DrawBoolSwitcher(StateSwitcher<bool> switcher, string label, bool forceSelected)
		{
			EditorGUI.BeginChangeCheck();
			var value = DrawToggle(switcher.Current, label, out var rect, forceSelected);
			if (EditorGUI.EndChangeCheck())
				ApplyState(switcher, value);

			HandleLabelPing(switcher, rect);
			return rect;
		}

		private static void ApplyState<TState>(StateSwitcher<TState> switcher, TState value)
		{
			var affectedSwitchers = CollectAffectedSwitchers(switcher);
			Undo.RecordObjects(affectedSwitchers, "Change State Switcher State");
			switcher.Switch(value, true);

			foreach (var affectedSwitcher in affectedSwitchers)
			{
				if (affectedSwitcher == null)
					continue;

				PrefabUtility.RecordPrefabInstancePropertyModifications(affectedSwitcher);
				EditorUtility.SetDirty(affectedSwitcher);
			}
		}

		private static IStateSwitcher FindLeafTarget(Transform target)
		{
			IStateSwitcher closestGroupSwitcher = null;

			if (!target.TryGetComponent(out IStateSwitcher switcher))
			{
				var parent = target.parent;
				if (parent != null)
				{
					for (int i = 0; i < parent.childCount; i++)
					{
						var transform = parent.GetChild(i);
						if (transform.TryGetComponent(out switcher))
						{
							target = transform;
							break;
						}
					}
				}
			}

			using (ListPool<IStateSwitcher>.Get(out var stateSwitchers))
			{
				stateSwitchers.Clear();

				while (target != null)
				{
					target.GetComponents(stateSwitchers);
					foreach (var stateSwitcher in stateSwitchers)
					{
						if (!IsSupported(stateSwitcher.StateType))
							continue;

						if (stateSwitcher is IGroupStateSwitcher)
							return stateSwitcher;

						closestGroupSwitcher ??= stateSwitcher;
					}

					target = target.parent;
				}

				return closestGroupSwitcher;
			}
		}

		private static IEnumerable<IStateSwitcher> FindParentTargets(IStateSwitcher target)
		{
			using (HashSetPool<IStateSwitcher>.Get(out var visited))
			{
				visited.Add(target);
				var current = target.Parent;

				while (current != null && visited.Add(current))
				{
					yield return current;
					current = current.Parent;
				}
			}
		}

		private static bool IsSupported(Type type)
			=> type == typeof(string) || type == typeof(int) || type == typeof(bool);

		private static IEnumerable<string> CollectStringVariants(StateSwitcher<string> switcher, HashSet<object> visited)
		{
			if (switcher == null || !visited.Add(switcher))
				yield break;

			var variants = switcher.GetVariants();
			if (variants != null)
			{
				foreach (var variant in variants)
				{
					if (variant == null)
						continue;

					var text = variant.ToString();
					if (!string.IsNullOrEmpty(text))
						yield return text;
				}
			}
		}

		private static UnityEngine.Object[] CollectAffectedSwitchers<TState>(StateSwitcher<TState> switcher)
		{
			using (ListPool<UnityEngine.Object>.Get(out var affected))
			using (HashSetPool<StateSwitcher<TState>>.Get(out var visited))
			{
				CollectAffectedSwitchersRecursive(switcher, affected, visited);
				return affected.ToArray();
			}
		}

		private static void CollectAffectedSwitchersRecursive<TState>(
			StateSwitcher<TState> switcher,
			List<UnityEngine.Object> affected,
			HashSet<StateSwitcher<TState>> visited
		)
		{
			if (switcher == null || !visited.Add(switcher))
				return;

			affected.Add(switcher);

			if (switcher is not IGroupStateSwitcher groupSwitcher)
				return;

			foreach (var item in groupSwitcher.Children)
			{
				if (item is StateSwitcher<TState> childSwitcher)
					CollectAffectedSwitchersRecursive(childSwitcher, affected, visited);
			}
		}

		private static Color _selectedBgColor = new Color(1f, 1f, 1f, 0.05f);

		private static int DrawIntField(int value, string label, out Rect rect, bool forceSelected = false)
		{
			rect = GetControlRect();
			if (forceSelected)
				EditorGUI.DrawRect(rect, _selectedBgColor);
			value = string.IsNullOrEmpty(label)
				? EditorGUI.IntField(rect, value)
				: EditorGUI.IntField(rect, label, value);
			return value;
		}

		private static bool DrawToggle(bool value, string label, out Rect rect, bool forceSelected = false)
		{
			rect = GetControlRect();
			if (forceSelected)
				EditorGUI.DrawRect(rect, _selectedBgColor);
			value = string.IsNullOrEmpty(label)
				? EditorGUI.Toggle(rect, value)
				: EditorGUI.Toggle(rect, label, value);
			return value;
		}

		private void HandleLabelPing(IStateSwitcher switcher, Rect rect)
		{
			if (!_isExpanded || switcher is not Component component)
				return;

			var labelRect = rect;
			labelRect.xMin  += PING_LABEL_LEFT_PADDING;
			labelRect.width =  Mathf.Min(EditorGUIUtility.labelWidth, rect.width);
			labelRect.width =  Mathf.Max(0f, labelRect.width - PING_LABEL_LEFT_PADDING);

			EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.Link);

			if (!GUI.Button(labelRect, GUIContent.none, GUIStyle.none))
				return;

			EditorGUIUtility.PingObject(component.gameObject);
		}

		private Dropdown GetStringDropdownField(StateSwitcher<string> switcher)
		{
			if (_switcherToDropdown.TryGetValue(switcher, out var dropdownField))
				return dropdownField;

			dropdownField = new Dropdown(value => ApplyState(switcher, value));
			_switcherToDropdown.Add(switcher, dropdownField);
			return dropdownField;
		}

		private static Rect GetControlRect() => EditorGUILayout.GetControlRect();

		private sealed class Dropdown
		{
			private readonly Action<string> _onSelected;

			private int _cacheKeyCount = -1;
			private bool? _showedSelectorBeforeClick;
			private GUIPopupSelector<string> _selector;

			public Dropdown(Action<string> onSelected)
			{
				_onSelected = onSelected;
			}

			public void Reset()
			{
				_selector?.Hide(true);
				_selector                  = null;
				_cacheKeyCount             = -1;
				_showedSelectorBeforeClick = null;
			}

			public string Draw(IList<string> variants, string selectedKey, string label, out Rect rect, bool forceSelected)
			{
				TryCreateSelector(variants, selectedKey);

				label ??= string.Empty;
				EditorGUILayout.GetControlRect();
				rect = GUILayoutUtility.GetLastRect();

				if (forceSelected)
					EditorGUI.DrawRect(rect, _selectedBgColor);

				if (_selector == null)
					return string.IsNullOrEmpty(label)
						? EditorGUI.TextField(rect, selectedKey)
						: EditorGUI.TextField(rect, label, selectedKey);

				var contains = string.IsNullOrEmpty(selectedKey) || variants.Contains(selectedKey);

				var selectorPopupRect = rect;
				var textFieldPosition = rect;
				var trianglePosition = rect.AlignRight(9f, 5f);

				if (trianglePosition.Contains(Event.current.mousePosition))
					_showedSelectorBeforeClick ??= _selector.show;

				if (GUI.Button(trianglePosition, GUIContent.none, GUIStyle.none))
				{
					var click = !_showedSelectorBeforeClick ?? true;
					if (click)
						_selector.ShowPopup(selectorPopupRect);

					_showedSelectorBeforeClick = null;
				}

				EditorGUIUtility.AddCursorRect(trianglePosition, MouseCursor.Link);
				var originalColor = GUI.color;
				if (!contains)
					GUI.color = SirenixGUIStyles.YellowWarningColor;

				var nextValue = SirenixEditorFields.TextField(textFieldPosition, new GUIContent(label), selectedKey);
				GUI.color = originalColor;

				if (!_selector.show)
					SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretDownFill);
				else
					SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretUpFill);

				EditorGUIUtility.AddCursorRect(trianglePosition, MouseCursor.Link);

				return nextValue;
			}

			private void TryCreateSelector(IList<string> variants, string selectedKey)
			{
				if (variants == null || variants.Count == 0)
				{
					Reset();
					return;
				}

				if (_selector != null && _cacheKeyCount == variants.Count)
					return;

				_selector = CreateSelector(variants, selectedKey);
			}

			private GUIPopupSelector<string> CreateSelector(IList<string> variants, string selectedKey)
			{
				_cacheKeyCount = variants.Count;
				using var _ = ListPool<string>.Get(out var keys);
				keys.Add(string.Empty);
				foreach (var key in variants)
					keys.Add(key);

				var selector = new GUIPopupSelector<string>(
					keys.ToArray(),
					selectedKey,
					_onSelected,
					pathEvaluator: static key => string.IsNullOrEmpty(key) ? NONE : key);

				selector.SetSearchFunction(item =>
				{
					if (item?.Value == null)
						return false;

					var key = (string) item.Value;
					return key.Contains(selector.GetSearchTerm().ToLower());
				});

				return selector;
			}
		}
	}
}
