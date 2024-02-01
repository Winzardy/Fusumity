using System;
using System.Collections.Generic;
using Fusumity.Editor.Extensions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fusumity.Editor.Assistance
{
	public struct AssetSelectorDrawData
	{
		private const string FIELD_CONTROL_PREFIX = "AssetField";
		public string NoAssetString => $"None ({targetType.Name})";

		private readonly Action<Object> _onSelected;

		public Object target;
		public string AssetName => target == null ? NoAssetString : target.name;
		public string newGuid;
		public readonly Type targetType;

		public readonly bool isComponent;

		private readonly GUIContent _label;
		private Rect _assetDropDownRect;

		private Texture _caretTexture;

		public AssetSelectorDrawData(Object target, GUIContent label, Type targetType, Action<Object> onSelected)
		{
			this._onSelected = onSelected;
			this.target = target;
			this.targetType = targetType;
			isComponent = typeof(Component).IsAssignableFrom(targetType);
			_label = label;

			newGuid = default;
			_caretTexture = default;
			_assetDropDownRect = default;
		}

		public bool IsValidObject(Object target)
		{
			if (isComponent)
			{
				return (target as GameObject)!.GetComponent(targetType) != null;
			}

			return true;
		}

		public void Draw(Rect position)
		{
			var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target));

			_assetDropDownRect = EditorGUI.PrefixLabel(position, _label);

			DrawControl(AssetName, guid);
		}

		internal void ApplySelectionChanges(string guid)
		{
			if (!string.IsNullOrEmpty(newGuid))
			{
				if (newGuid == NoAssetString)
				{
					target = null;
					newGuid = string.Empty;
				}
				else if (guid != newGuid)
				{
					var path = AssetDatabase.GUIDToAssetPath(newGuid);
					target = AssetDatabase.LoadAssetAtPath<Object>(path);
					newGuid = string.Empty;
				}

				_onSelected?.Invoke(target);
			}
		}

		private void DrawControl(string nameToUse, string guid)
		{
			const float pickerWidth = 12f;
			var pickerRect = _assetDropDownRect;
			pickerRect.width = pickerWidth;
			pickerRect.x = _assetDropDownRect.xMax - pickerWidth * 1.33f;

			var isPickerPressed = Event.current.type == EventType.MouseDown && Event.current.button == 0 && pickerRect.Contains(Event.current.mousePosition);

			if (target != null)
			{
				var iconHeight = EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing * 3;
				var iconSize = EditorGUIUtility.GetIconSize();
				EditorGUIUtility.SetIconSize(new Vector2(iconHeight, iconHeight));
				var assetPath = AssetDatabase.GUIDToAssetPath(guid);
				var assetIcon = AssetDatabase.GetCachedIcon(assetPath) as Texture2D;

				GUI.SetNextControlName(FIELD_CONTROL_PREFIX);
				if (EditorGUI.DropdownButton(_assetDropDownRect, new GUIContent(nameToUse, assetIcon), FocusType.Keyboard, EditorStyles.objectField))
				{
					if (Event.current.clickCount == 1)
					{
						GUI.FocusControl(FIELD_CONTROL_PREFIX);
						EditorGUIUtility.PingObject(target);
					}

					if (Event.current.clickCount == 2)
					{
						AssetDatabase.OpenAsset(target);
						GUIUtility.ExitGUI();
					}
				}

				EditorGUIUtility.SetIconSize(iconSize);
			}
			else
			{
				GUI.SetNextControlName(FIELD_CONTROL_PREFIX);
				if (EditorGUI.DropdownButton(_assetDropDownRect, new GUIContent(nameToUse), FocusType.Keyboard, EditorStyles.objectField))
					GUI.FocusControl(FIELD_CONTROL_PREFIX);
			}

			DrawCaret(pickerRect);

			if (isPickerPressed)
			{
				EditorWindow.GetWindow<AssetPopup>(true, "Select Asset").Initialize(this, guid, Event.current.mousePosition);
			}
		}

		private void DrawCaret(Rect pickerRect)
		{
			if (_caretTexture == null)
			{
				_caretTexture = EditorGUIUtility.IconContent("d_pick").image;
			}

			if (_caretTexture != null)
			{
				GUI.DrawTexture(pickerRect, _caretTexture, ScaleMode.ScaleToFit);
			}
		}
	}

	public class AssetPopup : EditorWindow
	{
		private AssetTreeView _tree;
		private TreeViewState _treeState;
		private bool _shouldClose;

		void ForceClose()
		{
			_shouldClose = true;
		}

		private string _currentName = string.Empty;
		private AssetSelectorDrawData _drawer;
		private string _guid;

		private SearchField _searchField;

		public void Initialize(AssetSelectorDrawData drawer, string guid, Vector2 mouseLocation)
		{
			_drawer = drawer;
			_guid = guid;
			_searchField = new SearchField();
			_shouldClose = false;

			var rect = position;
			mouseLocation = GUIUtility.GUIToScreenPoint(mouseLocation);
			if (mouseLocation.x < 0 && mouseLocation.x > -rect.width)
				mouseLocation.x = -rect.width;

			rect.position = mouseLocation;
			position = rect;

			_searchField.SetFocus();
			_searchField.downOrUpArrowKeyPressed += () => { _tree.SetFocus(); };

			if (_tree != null)
				_tree.SetInitialSelection(_drawer.AssetName);
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
				_tree = new AssetTreeView(_treeState, _drawer, this, _guid);
				_tree.Reload();
				_tree.SetInitialSelection(_drawer.AssetName);
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

		private sealed class AssetTreeViewItem : TreeViewItem
		{
			public readonly string assetPath;

			private string _guid;

			public string Guid
			{
				get
				{
					if (string.IsNullOrEmpty(_guid))
						_guid = AssetDatabase.AssetPathToGUID(assetPath);
					return _guid;
				}
			}

			public AssetTreeViewItem(int id, int depth, string displayName, string path)
				: base(id, depth, displayName)
			{
				assetPath = path;
				icon = AssetDatabase.GetCachedIcon(path) as Texture2D;
			}
		}

		private class AssetTreeView : TreeView
		{
			private AssetSelectorDrawData _drawer;
			private readonly AssetPopup _popup;
			private readonly string _guid;

			internal bool IsEnterKeyPressed { get; set; }

			public AssetTreeView(TreeViewState state, AssetSelectorDrawData drawer, AssetPopup popup, string guid) : base(state)
			{
				_drawer = drawer;
				_popup = popup;
				showBorder = true;
				showAlternatingRowBackgrounds = true;
				_guid = guid;
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
				var assetRefItem = FindItem(id, rootItem) as AssetTreeViewItem;
				if (assetRefItem != null && assetRefItem.Guid == _guid)
					_popup.ForceClose();
				else if (assetRefItem != null && !string.IsNullOrEmpty(assetRefItem.assetPath))
				{
					_drawer.newGuid = assetRefItem.Guid;
					if (string.IsNullOrEmpty(_drawer.newGuid))
						_drawer.newGuid = assetRefItem.assetPath;
				}
				else
				{
					_drawer.newGuid = _drawer.NoAssetString;
				}

				_popup.ForceClose();
			}

			protected override void SelectionChanged(IList<int> selectedIds)
			{
				if (selectedIds != null && selectedIds.Count == 1)
				{
					var oldGuid = _drawer.newGuid;
					var assetRefItem = FindItem(selectedIds[0], rootItem) as AssetTreeViewItem;
					if (assetRefItem != null && !string.IsNullOrEmpty(assetRefItem.assetPath))
					{
						_drawer.newGuid = assetRefItem.Guid;
						if (string.IsNullOrEmpty(_drawer.newGuid))
							_drawer.newGuid = assetRefItem.assetPath;
					}
					else
					{
						_drawer.newGuid = _drawer.NoAssetString;
					}

					SetFocus();
					_drawer.ApplySelectionChanges(oldGuid);
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

				root.AddChild(new AssetTreeViewItem(_drawer.NoAssetString.GetHashCode(), 0, _drawer.NoAssetString, string.Empty));
				var targetType = _drawer.isComponent ? typeof(GameObject) : _drawer.targetType;
				var allAssets = AssetsEditorExt.GetAssetsOfTypeWithPath(targetType, _drawer.IsValidObject);

				foreach (var (entry, path) in allAssets)
				{
					var child = new AssetTreeViewItem(entry.GetHashCode(), 0, entry.name, path);
					root.AddChild(child);
				}

				return root;
			}

			public void SetInitialSelection(string assetString)
			{
				foreach (var child in rootItem.children)
				{
					if (child.displayName.IndexOf(assetString, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						SetSelection(new List<int> { child.id });
						return;
					}
				}
			}
		}
	}
}