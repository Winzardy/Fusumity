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
		public int AssetId => target == null ? 0 : target.GetInstanceID();
		public string oldGuid;
		public readonly Type targetType;

		public readonly bool isComponent;

		private readonly GUIContent _label;
		private Rect _position;

		private Texture _caretTexture;

		public AssetSelectorDrawData(Object target, GUIContent label, Type targetType, Action<Object> onSelected)
		{
			this._onSelected = onSelected;
			this.target = target;
			this.targetType = targetType;
			isComponent = typeof(Component).IsAssignableFrom(targetType);
			_label = label;

			oldGuid = default;
			_caretTexture = default;
			_position = default;
		}

		public bool IsValidObject(Object objectToCheck)
		{
			if (isComponent)
			{
				return (objectToCheck as GameObject)!.GetComponent(targetType) != null;
			}

			return true;
		}

		public void Draw(Rect position)
		{
			var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target));

			_position = position;

			DrawControl(guid);
		}

		internal void ApplySelectionChanges(string newGuid)
		{
			if (!string.IsNullOrEmpty(newGuid))
			{
				if (newGuid == NoAssetString)
				{
					target = null;
					oldGuid = string.Empty;
				}
				else if (newGuid != oldGuid)
				{
					var path = AssetDatabase.GUIDToAssetPath(newGuid);
					target = AssetDatabase.LoadAssetAtPath<Object>(path);
					oldGuid = string.Empty;
				}
				else
				{
					oldGuid = newGuid;
				}

				_onSelected?.Invoke(target);
			}
		}

		private void DrawControl(string guid)
		{
			const float pickerWidth = 15f;
			var pickerRect = _position;
			pickerRect.x = pickerRect.xMax - pickerWidth * 1.33f;
			pickerRect.width = pickerWidth;

			var isPickerPressed = Event.current.type == EventType.MouseDown && Event.current.button == 0 && pickerRect.Contains(Event.current.mousePosition);

			DrawCaret(pickerRect);

			if (isPickerPressed)
			{
				EditorWindow.GetWindow<AssetPopup>(true, "Select Asset").Initialize(this, guid, Event.current.mousePosition);
			}
			else
			{
				var newTarget = EditorGUI.ObjectField(_position, _label, target, targetType, true);
				if (newTarget != target)
				{
					var newGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newTarget));
					ApplySelectionChanges(newGuid);
				}
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
				_tree.SetInitialSelection(_drawer.AssetId);
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
				_tree.SetInitialSelection(_drawer.AssetId);
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
					_drawer.oldGuid = assetRefItem.Guid;
					if (string.IsNullOrEmpty(_drawer.oldGuid))
						_drawer.oldGuid = assetRefItem.assetPath;
				}
				else
				{
					_drawer.oldGuid = _drawer.NoAssetString;
				}

				_popup.ForceClose();
			}

			protected override void SelectionChanged(IList<int> selectedIds)
			{
				if (selectedIds != null && selectedIds.Count == 1)
				{
					string newGuid;
					var assetRefItem = FindItem(selectedIds[0], rootItem) as AssetTreeViewItem;
					if (assetRefItem != null && !string.IsNullOrEmpty(assetRefItem.assetPath))
					{
						newGuid = assetRefItem.Guid;
						if (string.IsNullOrEmpty(newGuid))
							newGuid = assetRefItem.assetPath;
					}
					else
					{
						newGuid = _drawer.NoAssetString;
					}

					SetFocus();
					_drawer.ApplySelectionChanges(newGuid);
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

				root.AddChild(new AssetTreeViewItem(0, 0, _drawer.NoAssetString, string.Empty));
				var allAssets = _drawer.isComponent ?
					AssetsEditorExt.GetAssetsOfComponentTypeWithPath(_drawer.targetType) :
					AssetsEditorExt.GetAssetsOfTypeWithPath(_drawer.targetType);

				foreach (var (entry, path) in allAssets)
				{
					var child = new AssetTreeViewItem(entry.GetInstanceID(), 0, entry.name, path);
					root.AddChild(child);
				}

				return root;
			}

			public void SetInitialSelection(int assetId)
			{
				foreach (var child in rootItem.children)
				{
					if (child.id == assetId)
					{
						SetSelection(new List<int> { child.id });
						return;
					}
				}
			}
		}
	}
}