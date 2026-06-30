using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Editor;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sapientia.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace UI.Editor
{
	using UnityObject = UnityEngine.Object;

	[Overlay(typeof(SceneView), "State Switcher", "State Switcher")]
	public class StateSwitcherEditorOverlay : IMGUIOverlay, ITransientOverlay
	{
		private const float PING_LABEL_LEFT_PADDING = 18f;
		private const float TREE_INDENT_WIDTH = 18f;
		private const float TREE_FOLDOUT_WIDTH = 16f;

		private const float RIGHT_PADDING = 8f;
		private const float MIN_OVERLAY_WIDTH = 255f;
		private const float DEFAULT_OVERLAY_WIDTH = 360f;
		private const float MAX_OVERLAY_WIDTH = 900f;
		private const float MAX_OVERLAY_HEIGHT = 4096f;
		private const float BOTTOM_PADDING = 18f;
		private const int ROOT_CONTAINER_COLLAPSED_THRESHOLD = 3;

		private const string NONE = "None";
		private const string TOOL_ENABLED_EDITOR_PREF_KEY = "StateSwitcherEditorOverlay.Enabled";

		private static readonly Dictionary<string, PropertyInfo> _overlayVectorPropertyByName = new();
		private static readonly Color _selectedBgColor = new(1f, 1f, 1f, 0.05f);

		private static readonly GUIContent _toggleLabel = new GUIContent("Enable", "Включить/выключить инструмент");

		private readonly List<SwitcherTreeNode> _switcherNodes = new();
		private readonly Dictionary<IStateSwitcher, SwitcherTreeNode> _switcherToNode = new();
		private readonly Dictionary<Transform, SwitcherTreeNode> _containerToNode = new();
		private readonly Dictionary<IStateSwitcher, Dropdown> _switcherToDropdown = new();
		private readonly Dictionary<int, bool> _foldoutStateByNodeKey = new();
		private readonly Dictionary<int, bool> _treeFoldoutStateBySignature = new();
		private bool _isToolEnabled = true;
		private bool _isTreeExpanded = true;
		private float _overlayHeight;
		private GameObject _targetGameObject;
		private IStateSwitcher _targetSwitcherRoot;
		private string _treeRootLabel;
		private int _treeSignature;
		private SwitcherTreeNode _rootNode;

		public bool visible => !Application.isPlaying &&
			(!_isToolEnabled || _rootNode != null && _rootNode.Children.Count > 0);

		public override void OnCreated()
		{
			LoadToolEnabled();
			ApplyOverlaySizeBounds();
			Selection.selectionChanged += OnSelectionChanged;
			OnSelectionChanged();
		}

		public override void OnWillBeDestroyed()
		{
			Selection.selectionChanged -= OnSelectionChanged;
			ClearTargets(true);
		}

		private void OnSelectionChanged()
		{
			if (!_isToolEnabled)
			{
				CaptureFoldoutStates();
				ClearTargets(false);
				return;
			}

			if (Selection.activeGameObject == null)
			{
				CaptureFoldoutStates();
				ClearTargets(false);
				return;
			}

			if (Selection.activeGameObject.TryGetComponent(out IStateSwitcher stateSwitcher))
			{
				UpdateSwitcherTargets(stateSwitcher);
				return;
			}

			UpdateLayoutTargets(Selection.activeGameObject);
		}

		private void UpdateLayoutTargets(GameObject target)
		{
			_treeRootLabel = target.name;

			using (ListPool<IStateSwitcher>.Get(out var stateSwitchers))
			using (HashSetPool<IStateSwitcher>.Get(out var visited))
			{
				CollectLayoutSwitchers(target, stateSwitchers, visited);
				if (stateSwitchers.Count == 0)
				{
					CaptureFoldoutStates();
					ClearTargets(false);
					return;
				}

				var treeSignature = GetTreeSignature(target.GetInstanceID(), stateSwitchers);
				if (_targetGameObject == target &&
					_targetSwitcherRoot == null &&
					_treeSignature == treeSignature &&
					_rootNode != null)
					return;

				CaptureFoldoutStates();
				ClearTargets(false);

				_targetGameObject = target;
				_targetSwitcherRoot = null;
				_treeSignature = treeSignature;
				FindLayoutTargets(target, stateSwitchers);
			}
		}

		private void LoadToolEnabled()
		{
			_isToolEnabled = EditorPrefs.GetBool(TOOL_ENABLED_EDITOR_PREF_KEY, true);
		}

		private void SetToolEnabled(bool value)
		{
			if (_isToolEnabled == value)
				return;

			_isToolEnabled = value;
			EditorPrefs.SetBool(TOOL_ENABLED_EDITOR_PREF_KEY, _isToolEnabled);

			if (_isToolEnabled)
				OnSelectionChanged();
			else
			{
				CaptureFoldoutStates();
				ClearTargets(false);
			}

			SceneView.RepaintAll();
		}

		private void UpdateSwitcherTargets(IStateSwitcher stateSwitcher)
		{
			if (stateSwitcher is Component selectedComponent)
				_treeRootLabel = selectedComponent.gameObject.name;

			var rootSwitcher = FindRootSwitcher(stateSwitcher);
			if (rootSwitcher == null)
			{
				CaptureFoldoutStates();
				ClearTargets(false);
				return;
			}

			using (ListPool<IStateSwitcher>.Get(out var stateSwitchers))
			using (HashSetPool<IStateSwitcher>.Get(out var visited))
			{
				stateSwitchers.Clear();
				CollectSwitcherTree(rootSwitcher, stateSwitchers, visited);
				if (stateSwitchers.Count == 0)
				{
					CaptureFoldoutStates();
					ClearTargets(false);
					return;
				}

				var treeSignature = GetTreeSignature(GetSwitcherInstanceId(rootSwitcher), stateSwitchers);
				if (_targetGameObject == null &&
					ReferenceEquals(_targetSwitcherRoot, rootSwitcher) &&
					_treeSignature == treeSignature &&
					_rootNode != null)
					return;

				CaptureFoldoutStates();
				ClearTargets(false);

				_targetGameObject = null;
				_targetSwitcherRoot = rootSwitcher;
				_treeSignature = treeSignature;
				FindSwitcherTargets(rootSwitcher, stateSwitchers);
			}
		}

		private static IStateSwitcher FindRootSwitcher(IStateSwitcher stateSwitcher)
		{
			if (stateSwitcher is not Component component || !component || !IsSupported(stateSwitcher.StateType))
				return null;

			using (HashSetPool<IStateSwitcher>.Get(out var visited))
			{
				var current = stateSwitcher;
				while (current?.Parent != null &&
					current.Parent is Component parentComponent &&
					parentComponent &&
					IsSupported(current.Parent.StateType) &&
					visited.Add(current.Parent))
					current = current.Parent;

				return current;
			}
		}

		private static void CollectSwitcherTree(
			IStateSwitcher stateSwitcher,
			List<IStateSwitcher> stateSwitchers,
			HashSet<IStateSwitcher> visited
		)
		{
			if (stateSwitcher is not Component component || !component)
				return;

			if (!IsSupported(stateSwitcher.StateType) || !visited.Add(stateSwitcher))
				return;

			stateSwitchers.Add(stateSwitcher);

			if (stateSwitcher is not IGroupStateSwitcher groupSwitcher)
				return;

			var children = groupSwitcher.Children;
			if (children == null)
				return;

			foreach (var child in children)
				CollectSwitcherTree(child, stateSwitchers, visited);
		}

		private static void CollectLayoutSwitchers(
			GameObject target,
			List<IStateSwitcher> stateSwitchers,
			HashSet<IStateSwitcher> visited
		)
		{
			stateSwitchers.Clear();
			target.transform.GetComponentsInChildren(true, stateSwitchers);

			var writeIndex = 0;
			for (var i = 0; i < stateSwitchers.Count; i++)
			{
				var stateSwitcher = stateSwitchers[i];
				if (stateSwitcher is not Component component || !component)
					continue;

				if (!visited.Add(stateSwitcher) || !IsSupported(stateSwitcher.StateType))
					continue;

				stateSwitchers[writeIndex++] = stateSwitcher;
			}

			if (writeIndex < stateSwitchers.Count)
				stateSwitchers.RemoveRange(writeIndex, stateSwitchers.Count - writeIndex);
		}

		private void FindLayoutTargets(GameObject target, List<IStateSwitcher> stateSwitchers)
		{
			var root = target.transform;
			_rootNode = CreateNode(root);
			_containerToNode.Add(root, _rootNode);

			foreach (var stateSwitcher in stateSwitchers)
			{
				var node = CreateNode(stateSwitcher);
				_switcherNodes.Add(node);
				_switcherToNode.Add(stateSwitcher, node);
			}

			using (HashSetPool<SwitcherTreeNode>.Get(out var attached))
			{
				AttachGroupChildren(attached);
				foreach (var node in _switcherNodes)
					TryAttachRootSwitcher(root, node, attached);
			}

			AttachUnreachableSwitchers(root);
			ReparentContainerNodes(root);
			ApplyTreeRootFoldoutDefault();
		}

		private void FindSwitcherTargets(IStateSwitcher rootSwitcher, List<IStateSwitcher> stateSwitchers)
		{
			var rootTransform = ((Component) rootSwitcher).transform;
			var root = rootTransform.parent ? rootTransform.parent : rootTransform;
			_rootNode = CreateNode(root);
			_containerToNode.Add(root, _rootNode);

			foreach (var stateSwitcher in stateSwitchers)
			{
				var node = CreateNode(stateSwitcher);
				_switcherNodes.Add(node);
				_switcherToNode.Add(stateSwitcher, node);
			}

			using (HashSetPool<SwitcherTreeNode>.Get(out var attached))
			{
				AttachGroupChildren(attached);
				foreach (var node in _switcherNodes)
					TryAttachSwitcherRootNode(node, attached);
			}

			AttachUnreachableSwitchersToRoot();
			ApplyTreeRootFoldoutDefault();
		}

		private void AttachGroupChildren(HashSet<SwitcherTreeNode> attached)
		{
			foreach (var node in _switcherNodes)
			{
				if (node.Switcher is not IGroupStateSwitcher groupSwitcher)
					continue;

				var children = groupSwitcher.Children;
				if (children == null)
					continue;

				foreach (var child in children)
					TryAttachChild(node, child, attached);
			}
		}

		private void TryAttachRootSwitcher(Transform root, SwitcherTreeNode node, HashSet<SwitcherTreeNode> attached)
		{
			if (attached.Contains(node))
				return;

			var parent = node.Switcher.Parent;
			if (parent != null && _switcherToNode.TryGetValue(parent, out var parentNode))
			{
				TryAttachChild(parentNode, node, attached);
				return;
			}

			var container = GetOrCreateContainerNode(root, node.Transform);
			container.Children.Add(node);
			attached.Add(node);
		}

		private void TryAttachSwitcherRootNode(SwitcherTreeNode node, HashSet<SwitcherTreeNode> attached)
		{
			if (attached.Contains(node))
				return;

			var parent = node.Switcher.Parent;
			if (parent != null && _switcherToNode.TryGetValue(parent, out var parentNode))
			{
				TryAttachChild(parentNode, node, attached);
				return;
			}

			_rootNode.Children.Add(node);
			attached.Add(node);
		}

		private SwitcherTreeNode CreateNode(Transform transform)
		{
			var node = new SwitcherTreeNode(transform);
			RestoreFoldoutState(node);
			return node;
		}

		private SwitcherTreeNode CreateNode(IStateSwitcher stateSwitcher)
		{
			var node = new SwitcherTreeNode(stateSwitcher);
			RestoreFoldoutState(node);
			return node;
		}

		private void RestoreFoldoutState(SwitcherTreeNode node)
		{
			if (_foldoutStateByNodeKey.TryGetValue(node.Key, out var isExpanded))
			{
				node.IsExpanded = isExpanded;
				node.HasFoldoutState = true;
			}
		}

		private void AttachUnreachableSwitchers(Transform root)
		{
			using (HashSetPool<SwitcherTreeNode>.Get(out var reachable))
			{
				CollectReachableNodes(_rootNode, reachable);

				foreach (var node in _switcherNodes)
				{
					if (reachable.Contains(node))
						continue;

					var container = GetOrCreateContainerNode(root, node.Transform);
					if (!container.Children.Contains(node))
						container.Children.Add(node);

					CollectReachableNodes(node, reachable);
				}
			}
		}

		private void AttachUnreachableSwitchersToRoot()
		{
			using (HashSetPool<SwitcherTreeNode>.Get(out var reachable))
			{
				CollectReachableNodes(_rootNode, reachable);

				foreach (var node in _switcherNodes)
				{
					if (reachable.Contains(node))
						continue;

					if (!_rootNode.Children.Contains(node))
						_rootNode.Children.Add(node);

					CollectReachableNodes(node, reachable);
				}
			}
		}

		private static void CollectReachableNodes(SwitcherTreeNode node, HashSet<SwitcherTreeNode> reachable)
		{
			if (node == null || !reachable.Add(node))
				return;

			foreach (var child in node.Children)
				CollectReachableNodes(child, reachable);
		}

		private void ApplyTreeRootFoldoutDefault()
		{
			if (_treeFoldoutStateBySignature.TryGetValue(_treeSignature, out var isExpanded))
			{
				_isTreeExpanded = isExpanded;
				return;
			}

			_isTreeExpanded = CountContainerNodes(_rootNode, false) <= ROOT_CONTAINER_COLLAPSED_THRESHOLD;
		}

		private static int CountContainerNodes(SwitcherTreeNode node, bool includeSelf = true)
		{
			if (node == null)
				return 0;

			var count = includeSelf && node.Switcher == null ? 1 : 0;
			foreach (var child in node.Children)
				count += CountContainerNodes(child);

			return count;
		}

		private void CaptureFoldoutStates()
		{
			if (_rootNode == null)
				return;

			if (_treeSignature != 0)
				_treeFoldoutStateBySignature[_treeSignature] = _isTreeExpanded;

			using (HashSetPool<SwitcherTreeNode>.Get(out var visited))
				CaptureFoldoutStates(_rootNode, visited);
		}

		private void CaptureFoldoutStates(SwitcherTreeNode node, HashSet<SwitcherTreeNode> visited)
		{
			if (node == null || !visited.Add(node))
				return;

			_foldoutStateByNodeKey[node.Key] = node.IsExpanded;
			foreach (var child in node.Children)
				CaptureFoldoutStates(child, visited);
		}

		private static int GetTreeSignature(int rootKey, List<IStateSwitcher> stateSwitchers)
		{
			unchecked
			{
				var hash = 17;
				hash = hash * 31 + rootKey;

				foreach (var stateSwitcher in stateSwitchers)
				{
					var component = (Component) stateSwitcher;
					hash = hash * 31 + component.GetInstanceID();
					hash = hash * 31 + (component.transform.parent ? component.transform.parent.GetInstanceID() : 0);
					hash = hash * 31 + GetSwitcherInstanceId(stateSwitcher.Parent);

					if (stateSwitcher is not IGroupStateSwitcher groupSwitcher)
						continue;

					var children = groupSwitcher.Children;
					if (children == null)
						continue;

					foreach (var child in children)
					{
						if (child == null || !IsSupported(child.StateType))
							continue;

						hash = hash * 31 + GetSwitcherInstanceId(child);
					}
				}

				return hash;
			}
		}

		private static int GetSwitcherInstanceId(IStateSwitcher stateSwitcher)
			=> stateSwitcher is Component component && component ? component.GetInstanceID() : 0;

		private bool TryAttachChild(SwitcherTreeNode parent, IStateSwitcher child, HashSet<SwitcherTreeNode> attached)
		{
			if (child == null || !_switcherToNode.TryGetValue(child, out var childNode))
				return false;

			return TryAttachChild(parent, childNode, attached);
		}

		private static bool TryAttachChild(SwitcherTreeNode parent, SwitcherTreeNode child, HashSet<SwitcherTreeNode> attached)
		{
			if (parent == child || attached.Contains(child))
				return false;

			parent.Children.Add(child);
			attached.Add(child);
			return true;
		}

		private SwitcherTreeNode GetOrCreateContainerNode(Transform root, Transform target)
		{
			var container = GetSwitcherContainer(root, target);
			if (_containerToNode.TryGetValue(container, out var node))
				return node;

			node = CreateNode(container);
			_containerToNode.Add(container, node);
			AttachContainerNode(root, node);
			return node;
		}

		private void ReparentContainerNodes(Transform root)
		{
			using (ListPool<SwitcherTreeNode>.Get(out var containers))
			{
				foreach (var node in _containerToNode.Values)
				{
					if (node != _rootNode)
						containers.Add(node);
				}

				foreach (var node in containers)
					AttachContainerNode(root, node);
			}
		}

		private void AttachContainerNode(Transform root, SwitcherTreeNode node)
		{
			var parent = FindContainerParent(root, node.Transform);
			RemoveContainerNodeFromParents(node);

			if (!parent.Children.Contains(node))
				parent.Children.Add(node);
		}

		private SwitcherTreeNode FindContainerParent(Transform root, Transform container)
		{
			for (var current = container.parent; current != null && current != root; current = current.parent)
			{
				if (_containerToNode.TryGetValue(current, out var node))
					return node;
			}

			return _rootNode;
		}

		private void RemoveContainerNodeFromParents(SwitcherTreeNode node)
		{
			foreach (var parent in _containerToNode.Values)
			{
				if (parent != node)
					parent.Children.Remove(node);
			}
		}

		private static Transform GetSwitcherContainer(Transform root, Transform target)
		{
			if (target == root || target.parent == null)
				return root;

			if (!target.IsChildOf(root))
				return root;

			return target.parent;
		}

		private void ClearTargets(bool clearFoldoutStates)
		{
			foreach (var dropdownField in _switcherToDropdown.Values)
				dropdownField.Reset();
			_switcherToDropdown.Clear();

			_switcherNodes.Clear();
			_switcherToNode.Clear();
			_containerToNode.Clear();
			_targetGameObject = null;
			_targetSwitcherRoot = null;
			_treeRootLabel = null;
			_treeSignature = 0;
			_rootNode = null;

			if (clearFoldoutStates)
			{
				_foldoutStateByNodeKey.Clear();
				_treeFoldoutStateBySignature.Clear();
				_isTreeExpanded = true;
			}
		}

		public override void OnGUI()
		{
			if (Application.isPlaying)
				return;

			if (_isToolEnabled && (_rootNode == null || _rootNode.Children.Count == 0))
				return;

			var width = _isToolEnabled ? GetOverlayLayoutWidth() : MIN_OVERLAY_WIDTH;
			using (new GUILayout.VerticalScope(
				GUILayout.Width(width),
				GUILayout.MinWidth(MIN_OVERLAY_WIDTH),
				GUILayout.MaxWidth(MAX_OVERLAY_WIDTH)))
			{
				var rect = GUILayoutUtility.GetRect(width, 0f, GUILayout.Width(width));
				DrawToolToggle();

				if (_isToolEnabled && _rootNode != null && _rootNode.Children.Count > 0)
				{
					DrawTreeRootRow();
					if (_isTreeExpanded)
					{
						using (HashSetPool<SwitcherTreeNode>.Get(out var visited))
							DrawSwitcherTreeChildren(_rootNode, 1, visited);
					}

					GUILayout.Space(5);
				}

				UpdateOverlayHeight();
			}
		}

		private void DrawToolToggle()
		{
			GUILayout.BeginHorizontal();
			{
				EditorGUI.BeginChangeCheck();
				var value = GUILayout.Toggle(_isToolEnabled, GUIContent.none);
				GUILayout.BeginVertical();
				{
					GUILayout.Space(3);
					var width = EditorStyles.centeredGreyMiniLabel.CalcWidth(_toggleLabel.text);
					GUILayout.Label(_toggleLabel, EditorStyles.centeredGreyMiniLabel, GUILayout.Width( width));
				}
				GUILayout.EndVertical();
				if (EditorGUI.EndChangeCheck())
					SetToolEnabled(value);

				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();
		}

		private void DrawTreeRootRow()
		{
			using (new GUILayout.HorizontalScope())
			{
				var rect = GUILayoutUtility.GetRect(
					TREE_FOLDOUT_WIDTH,
					EditorGUIUtility.singleLineHeight,
					GUILayout.Width(TREE_FOLDOUT_WIDTH));

				_isTreeExpanded = EditorGUI.Foldout(rect, _isTreeExpanded, GUIContent.none, true);
				EditorGUILayout.LabelField(GetTreeRootLabel(), EditorStyles.boldLabel);
				GUILayout.Space(RIGHT_PADDING);
			}
		}

		private string GetTreeRootLabel()
			=> !_treeRootLabel.IsNullOrEmpty() ? _treeRootLabel : _rootNode?.Transform ? _rootNode.Transform.name : string.Empty;

		private void ApplyOverlaySizeBounds()
		{
			var fixedHeight = _overlayHeight > 0f ? _overlayHeight : MAX_OVERLAY_HEIGHT;
			TrySetOverlayVectorProperty("minSize", new Vector2(MIN_OVERLAY_WIDTH, _overlayHeight));
			TrySetOverlayVectorProperty("maxSize", new Vector2(MAX_OVERLAY_WIDTH, fixedHeight));
		}

		private float GetOverlayLayoutWidth()
		{
			if (!TryGetOverlayVectorProperty("size", out var property))
				return DEFAULT_OVERLAY_WIDTH;

			try
			{
				var size = (Vector2) property.GetValue(this);
				if (size.x > 0f)
					return Mathf.Clamp(size.x, MIN_OVERLAY_WIDTH, MAX_OVERLAY_WIDTH);
			}
			catch
			{
				_overlayVectorPropertyByName.Remove("size");
			}

			return DEFAULT_OVERLAY_WIDTH;
		}

		private void TrySetOverlayVectorProperty(string propertyName, Vector2 value)
		{
			if (!TryGetOverlayVectorProperty(propertyName, out var property))
				return;

			try
			{
				property.SetValue(this, value);
			}
			catch
			{
				_overlayVectorPropertyByName.Remove(propertyName);
			}
		}

		private static bool TryGetOverlayVectorProperty(string propertyName, out PropertyInfo property)
		{
			if (_overlayVectorPropertyByName.TryGetValue(propertyName, out property))
				return property != null;

			for (var type = typeof(StateSwitcherEditorOverlay); type != null; type = type.BaseType)
			{
				property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (property?.PropertyType != typeof(Vector2) || !property.CanWrite)
					continue;

				_overlayVectorPropertyByName[propertyName] = property;
				return true;
			}

			_overlayVectorPropertyByName[propertyName] = null;
			return false;
		}

		private void UpdateOverlayHeight()
		{
			if (Event.current.type != EventType.Repaint)
				return;

			var lastRect = GUILayoutUtility.GetLastRect();
			var height = Mathf.Clamp(lastRect.yMax + BOTTOM_PADDING, 0f, MAX_OVERLAY_HEIGHT);
			if (Mathf.Approximately(_overlayHeight, height))
				return;

			_overlayHeight = height;
			ApplyOverlaySizeBounds();
		}

		private void DrawSwitcherTreeNode(SwitcherTreeNode node, int depth, HashSet<SwitcherTreeNode> visited)
		{
			if (node == null || !visited.Add(node))
				return;

			DrawSwitcherTreeRow(node, depth);
			if (node.IsExpanded)
				DrawSwitcherTreeChildren(node, depth + 1, visited);
		}

		private void DrawSwitcherTreeChildren(SwitcherTreeNode node, int depth, HashSet<SwitcherTreeNode> visited)
		{
			foreach (var child in node.Children)
				DrawSwitcherTreeNode(child, depth, visited);
		}

		private void DrawSwitcherTreeRow(SwitcherTreeNode node, int depth)
		{
			using (new GUILayout.HorizontalScope())
			{
				if (depth > 0)
					GUILayout.Space(depth * TREE_INDENT_WIDTH);

				DrawFoldout(node);

				if (node.Switcher != null)
					DrawSwitcher(node.Switcher, GetSwitcherLabel(node, depth));
				else
					DrawTransformGroupLabel(node.Transform);

				GUILayout.Space(RIGHT_PADDING);
			}
		}

		private static void DrawFoldout(SwitcherTreeNode node)
		{
			var rect = GUILayoutUtility.GetRect(
				TREE_FOLDOUT_WIDTH,
				EditorGUIUtility.singleLineHeight,
				GUILayout.Width(TREE_FOLDOUT_WIDTH));

			if (node.Children.Count == 0)
				return;

			node.IsExpanded = EditorGUI.Foldout(rect, node.IsExpanded, GUIContent.none, true);
		}

		private static Rect DrawTransformGroupLabel(Transform target)
		{
			var rect = EditorGUILayout.GetControlRect();
			EditorGUI.LabelField(rect, target.name, EditorStyles.boldLabel);
			EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

			if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
				EditorGUIUtility.PingObject(target.gameObject);

			return rect;
		}

		private static string GetSwitcherLabel(SwitcherTreeNode node, int depth)
		{
			if (depth <= 0)
				return node.Switcher.Name;

			return $"{node.Switcher.Name}";
		}

		private Rect DrawSwitcher(IStateSwitcher target, string name, bool isSelected = false)
		{
			if (target is not Component component || !component)
				return default;

			switch (target)
			{
				case StateSwitcher<string> switcher:
					return DrawStringSwitcher(switcher, name, isSelected);

				case StateSwitcher<int> switcher:
					return DrawIntSwitcher(switcher, name, isSelected);

				case StateSwitcher<bool> switcher:
					return DrawBoolSwitcher(switcher, name, isSelected);
			}

			return default;
		}

		private Rect DrawStringSwitcher(StateSwitcher<string> switcher, string label, bool forceSelected = false)
		{
			using (ListPool<string>.Get(out var variants))
			using (HashSetPool<string>.Get(out var uniqueVariants))
			using (HashSetPool<object>.Get(out var visited))
			{
				CollectStringVariants(switcher, visited, uniqueVariants, variants);

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

		private static bool IsSupported(Type type)
			=> type == typeof(string) || type == typeof(int) || type == typeof(bool);

		private static void CollectStringVariants(
			StateSwitcher<string> switcher,
			HashSet<object> visited,
			HashSet<string> uniqueVariants,
			List<string> variants
		)
		{
			if (switcher == null || !visited.Add(switcher))
				return;

			CollectTextVariants(switcher.GetVariants(), uniqueVariants, variants);
			CollectDictionaryStringVariants(switcher, uniqueVariants, variants);

			if (switcher is not IGroupStateSwitcher groupSwitcher)
				return;

			var children = groupSwitcher.Children;
			if (children == null)
				return;

			foreach (var child in children)
			{
				if (child is StateSwitcher<string> childSwitcher)
					CollectStringVariants(childSwitcher, visited, uniqueVariants, variants);
			}
		}

		private static void CollectDictionaryStringVariants(
			StateSwitcher<string> switcher,
			HashSet<string> uniqueVariants,
			List<string> variants
		)
		{
			if (!switcher.TryFindFieldRecursively("_dictionary", out var dictField, BindingFlags.Instance | BindingFlags.NonPublic))
				return;

			if (dictField.GetValue(switcher) is not IDictionary dictionary)
				return;

			CollectTextVariants(dictionary.Keys, uniqueVariants, variants);
		}

		private static void CollectTextVariants(IEnumerable source, HashSet<string> uniqueVariants, List<string> variants)
		{
			if (source == null)
				return;

			foreach (var variant in source)
			{
				if (variant == null)
					continue;

				var text = variant.ToString();
				if (!text.IsNullOrEmpty() && uniqueVariants.Add(text))
					variants.Add(text);
			}
		}

		private static UnityObject[] CollectAffectedSwitchers<TState>(StateSwitcher<TState> switcher)
		{
			using (ListPool<UnityObject>.Get(out var affected))
			using (HashSetPool<StateSwitcher<TState>>.Get(out var visited))
			{
				CollectAffectedSwitchersRecursive(switcher, affected, visited);
				return affected.ToArray();
			}
		}

		private static void CollectAffectedSwitchersRecursive<TState>(
			StateSwitcher<TState> switcher,
			List<UnityObject> affected,
			HashSet<StateSwitcher<TState>> visited
		)
		{
			if (switcher == null || !visited.Add(switcher))
				return;

			affected.Add(switcher);

			if (switcher is not IGroupStateSwitcher groupSwitcher)
				return;

			var children = groupSwitcher.Children;
			if (children == null)
				return;

			foreach (var item in children)
			{
				if (item is StateSwitcher<TState> childSwitcher)
					CollectAffectedSwitchersRecursive(childSwitcher, affected, visited);
			}
		}

		private static int DrawIntField(int value, string label, out Rect rect, bool forceSelected = false)
		{
			rect = GetControlRect();
			if (forceSelected)
				EditorGUI.DrawRect(rect, _selectedBgColor);
			value = label.IsNullOrEmpty()
				? EditorGUI.IntField(rect, value)
				: EditorGUI.IntField(rect, label, value);
			return value;
		}

		private static bool DrawToggle(bool value, string label, out Rect rect, bool forceSelected = false)
		{
			rect = GetControlRect();
			if (forceSelected)
				EditorGUI.DrawRect(rect, _selectedBgColor);
			value = label.IsNullOrEmpty()
				? EditorGUI.Toggle(rect, value)
				: EditorGUI.Toggle(rect, label, value);
			return value;
		}

		private void HandleLabelPing(IStateSwitcher switcher, Rect rect)
		{
			if (switcher is not Component component)
				return;

			var labelRect = rect;
			labelRect.xMin += PING_LABEL_LEFT_PADDING;
			labelRect.width = Mathf.Min(EditorGUIUtility.labelWidth, rect.width);
			labelRect.width = Mathf.Max(0f, labelRect.width - PING_LABEL_LEFT_PADDING);

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

		private sealed class SwitcherTreeNode
		{
			public readonly Transform Transform;
			public readonly IStateSwitcher Switcher;
			public readonly List<SwitcherTreeNode> Children = new();
			public readonly int Key;
			public bool HasFoldoutState;
			public bool IsExpanded;

			public SwitcherTreeNode(Transform transform)
			{
				Transform = transform;
				Key = transform.GetInstanceID();
				IsExpanded = true;
			}

			public SwitcherTreeNode(IStateSwitcher switcher)
			{
				Switcher = switcher;
				Transform = ((Component) switcher).transform;
				Key = ((Component) switcher).GetInstanceID();
			}
		}

		private sealed class Dropdown
		{
			private readonly Action<string> _onSelected;

			private int _cacheKeyCount = -1;
			private int _cacheKeyHash;
			private bool? _showedSelectorBeforeClick;
			private GUIPopupSelector<string> _selector;

			public Dropdown(Action<string> onSelected)
			{
				_onSelected = onSelected;
			}

			public void Reset()
			{
				_selector?.Hide(true);
				_selector = null;
				_cacheKeyCount = -1;
				_cacheKeyHash = 0;
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
					return label.IsNullOrEmpty()
						? EditorGUI.TextField(rect, selectedKey)
						: EditorGUI.TextField(rect, label, selectedKey);

				var contains = selectedKey.IsNullOrEmpty() || variants.Contains(selectedKey);

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

				var cacheKeyHash = GetVariantsHash(variants);
				if (_selector != null && _cacheKeyCount == variants.Count && _cacheKeyHash == cacheKeyHash)
				{
					if (_selector.selectedValue != selectedKey)
						_selector.SetSelection(selectedKey);

					return;
				}

				_selector = CreateSelector(variants, selectedKey, cacheKeyHash);
			}

			private GUIPopupSelector<string> CreateSelector(IList<string> variants, string selectedKey, int cacheKeyHash)
			{
				_cacheKeyCount = variants.Count;
				_cacheKeyHash = cacheKeyHash;

				var keys = new string[variants.Count + 1];
				keys[0] = string.Empty;
				for (var i = 0; i < variants.Count; i++)
					keys[i + 1] = variants[i];

				var selector = new GUIPopupSelector<string>(
					keys,
					selectedKey,
					_onSelected,
					pathEvaluator: static key => key.IsNullOrEmpty() ? NONE : key);

				selector.SetSearchFunction(item =>
				{
					if (item?.Value == null)
						return false;

					var key = (string) item.Value;
					return key.Contains(selector.GetSearchTerm().ToLower());
				});

				return selector;
			}

			private static int GetVariantsHash(IList<string> variants)
			{
				unchecked
				{
					var hash = 17;
					foreach (var variant in variants)
						hash = hash * 31 + (variant?.GetHashCode() ?? 0);

					return hash;
				}
			}
		}
	}
}
