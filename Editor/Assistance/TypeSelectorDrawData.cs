using System;
using System.Collections.Generic;
using Fusumity.Editor.Extensions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Fusumity.Editor.Assistance
{
	public struct TypeSelectorDrawData
	{
		private const float COPY_PASTE_BUTTONS_WIDTH = 50f;
		public string NoTypeString => $"None ({targetType.Name})";
		public string Name => currentType?.Name ?? NoTypeString;

		private readonly Action<object> _onSelected;

		public SerializedProperty property;
		public readonly Type targetType;
		public Type currentType;

		public Type newType;

		public readonly bool insertNull;
		private readonly GUIContent _label;
		private Rect _objectDropDownRect;

		public TypeSelectorDrawData(SerializedProperty property, GUIContent label, Type targetType, Type currentType, bool insertNull = true)
			: this(property, label, targetType, currentType,null, insertNull)
		{
		}

		public TypeSelectorDrawData(SerializedProperty property, GUIContent label, Type targetType, Type currentType, Action<object> onSelected,
			bool insertNull = true)
		{
			this.property = property;
			this.targetType = targetType;
			this.currentType = currentType;
			_label = label;
			_onSelected = onSelected;

			this.insertNull = insertNull;
			newType = default;
			_objectDropDownRect = default;

			if (_onSelected == null)
				_onSelected = BaseOnSelected;

			if (!insertNull && currentType == null)
			{
				var types = targetType.GetInheritorTypes(false);
				if (types.Length == 0)
					return;
				newType = types[0];
				ApplySelectionChanges();
			}
		}

		private void BaseOnSelected(object target)
		{
			property.boxedValue = target;

			property.serializedObject.ApplyModifiedProperties();
			if (property.serializedObject.context != null)
				EditorUtility.SetDirty(property.serializedObject.context);
		}

		public void Draw(Rect position)
		{
			var dropdownPosition = position;
			dropdownPosition.xMax -= COPY_PASTE_BUTTONS_WIDTH;

			_objectDropDownRect = EditorGUI.PrefixLabel(dropdownPosition, _label);

			var copyPosition = position;
			copyPosition.xMin = copyPosition.xMax - COPY_PASTE_BUTTONS_WIDTH;
			copyPosition.xMax = copyPosition.xMax - COPY_PASTE_BUTTONS_WIDTH / 2;
			copyPosition.height = EditorGUIUtility.singleLineHeight;
			if (GUI.Button(copyPosition, "C"))
			{
				property.CopyValue();
			}

			var pastPosition = position;
			pastPosition.xMin = copyPosition.xMax;
			pastPosition.height = EditorGUIUtility.singleLineHeight;
			if (GUI.Button(pastPosition, "P"))
			{
				property.PasteValue();
			}

			DrawControl(currentType);
		}

		internal void ApplySelectionChanges()
		{
			object target = null;
			if (newType != null && currentType != newType)
			{
				target = Activator.CreateInstance(newType);
			}
			currentType = newType;

			_onSelected?.Invoke(target);
		}

		private void DrawControl(Type currenType)
		{
			var isPressed = EditorGUI.DropdownButton(_objectDropDownRect, new GUIContent(Name), FocusType.Keyboard);
			if (isPressed)
			{
				EditorWindow.GetWindow<ReferencePopup>(true, "Select Type").Initialize(this, currenType, Event.current.mousePosition);
			}
		}
	}

	public class ReferencePopup : EditorWindow
	{
		private TypeTreeView _tree;
		private TreeViewState _treeState;
		private bool _shouldClose;
		private Type _type;

		void ForceClose()
		{
			_shouldClose = true;
		}

		private string _currentName = string.Empty;
		private TypeSelectorDrawData _drawer;

		private SearchField _searchField;

		public void Initialize(TypeSelectorDrawData drawer, Type type, Vector2 mouseLocation)
		{
			_drawer = drawer;
			_searchField = new SearchField();
			_shouldClose = false;
			_type = type;

			var rect = position;
			mouseLocation = GUIUtility.GUIToScreenPoint(mouseLocation);
			if (mouseLocation.x < 0 && mouseLocation.x > -rect.width)
				mouseLocation.x = -rect.width;

			rect.position = mouseLocation;
			position = rect;

			_searchField.SetFocus();
			_searchField.downOrUpArrowKeyPressed += () => { _tree.SetFocus(); };

			if (_tree != null)
				_tree.SetInitialSelection(_drawer.Name);
		}

		private void OnLostFocus()
		{
			ForceClose();
		}

		private void OnGUI()
		{
			var rect = position;

			const int border = 4;
			const int topPadding = 12;
			const int searchHeight = 20;
			const int remainTop = topPadding + searchHeight + border;

			var searchRect = new Rect(border, topPadding, rect.width - border * 2, searchHeight);
			var remainingRect = new Rect(border, topPadding + searchHeight + border, rect.width - border * 2, rect.height - remainTop - border);

			_currentName = _searchField.OnGUI(searchRect, _currentName);

			if (_tree == null)
			{
				if (_treeState == null)
					_treeState = new TreeViewState();
				_tree = new TypeTreeView(_treeState, _drawer, this, _type);
				_tree.Reload();
				_tree.SetInitialSelection(_drawer.Name);
			}

			var isKeyPressed = Event.current.type == EventType.KeyDown && Event.current.isKey;
			var isEnterKeyPressed = isKeyPressed && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return);
			var isUpOrDownArrowPressed = isKeyPressed && (Event.current.keyCode == KeyCode.UpArrow || Event.current.keyCode == KeyCode.DownArrow);

			if (isUpOrDownArrowPressed)
				_tree.SetFocus();

			_tree.searchString = _currentName;
			_tree.IsEnterKeyPressed = isEnterKeyPressed;
			_tree.OnGUI(remainingRect);

			if (_shouldClose || isEnterKeyPressed)
			{
				GUIUtility.hotControl = 0;
				Close();
			}
		}

		private sealed class TypeTreeViewItem : TreeViewItem
		{
			public readonly Type type;

			public TypeTreeViewItem(int id, int depth, string displayName, Type type)
				: base(id, depth, displayName)
			{
				this.type = type;
			}
		}

		private class TypeTreeView : TreeView
		{
			private TypeSelectorDrawData _drawer;
			private readonly ReferencePopup _popup;
			private readonly Type _type;

			internal bool IsEnterKeyPressed { get; set; }

			public TypeTreeView(TreeViewState state, TypeSelectorDrawData drawer, ReferencePopup popup, Type type) : base(state)
			{
				_drawer = drawer;
				_popup = popup;
				showBorder = true;
				showAlternatingRowBackgrounds = true;
				_type = type;
			}

			public override void OnGUI(Rect rect)
			{
				base.OnGUI(rect);
				if (IsEnterKeyPressed && HasFocus())
				{
					_popup.ForceClose();
				}
			}

			protected override bool CanMultiSelect(TreeViewItem item)
			{
				return false;
			}

			protected override void DoubleClickedItem(int id)
			{
				var typeRefItem = FindItem(id, rootItem) as TypeTreeViewItem;
				if (typeRefItem != null && typeRefItem.type == _type)
					_popup.ForceClose();
				else
					_drawer.newType = typeRefItem?.type;

				_popup.ForceClose();
			}

			protected override void SelectionChanged(IList<int> selectedIds)
			{
				if (selectedIds != null && selectedIds.Count == 1)
				{
					var typeRefItem = FindItem(selectedIds[0], rootItem) as TypeTreeViewItem;
					_drawer.newType = typeRefItem?.type;

					SetFocus();
					_drawer.ApplySelectionChanges();
				}
			}

			protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
			{
				if (string.IsNullOrEmpty(searchString))
				{
					return base.BuildRows(root);
				}

				var rows = new List<TreeViewItem>();

				foreach (var child in rootItem.children)
				{
					if (child.displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
						rows.Add(child);
				}

				return rows;
			}

			protected override TreeViewItem BuildRoot()
			{
				var root = new TreeViewItem(-1, -1);

				if (_drawer.insertNull)
					root.AddChild(new TypeTreeViewItem(_drawer.NoTypeString.GetHashCode(), 0, _drawer.NoTypeString, null));

				if (_drawer.targetType.IsGenericType)
					AddChild(_drawer.targetType);

				var types = _drawer.targetType.GetInheritorTypes(false);
				foreach (var type in types)
				{
					AddChild(type);
				}

				return root;

				void AddChild(Type type)
				{
					if (type.IsAbstract || type.IsInterface)
						return;
					var child = new TypeTreeViewItem(type.GetHashCode(), 0, type.Name, type);
					root.AddChild(child);
				}
			}

			public void SetInitialSelection(string typeName)
			{
				foreach (var child in rootItem.children)
				{
					if (child.displayName == typeName)
					{
						SetSelection(new List<int> { child.id });
						return;
					}
				}
			}
		}
	}
}