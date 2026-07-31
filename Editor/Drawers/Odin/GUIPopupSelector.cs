using System;
using System.Collections.Generic;
using System.Linq;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	using UnityObject = UnityEngine.Object;

	public class GUIPopupSelector<T> : OdinSelector<T>
	{
		private bool _show;
		private OdinEditorWindow _window;

		private readonly T[] _values;

		private Func<T, string> _pathEvaluator;
		private Action<T> _onSelected;
		private Action<T, int> _onSelectedIndex;

		private bool _enableSearch;
		private bool _enableMultiselect;
		private bool _autoHideOnSelectionChanged;
		private OdinMenuTree _tree;

		private Dictionary<T, string> _cacheItemNames;
		private List<FunctionButtonInfo> _functionButtonsInfo;
		private List<FunctionButtonInfo> _toolbarFunctionButtonsInfo;

		private Func<OdinMenuItem, bool> _searchFunction;
		private Func<T, string> _nameEvaluator;
		private Func<T, Sprite> _iconEvaluator;
		private Func<T, string> _secondaryLabelEvaluator;

		public bool show => _show;
		public OdinEditorWindow Window => _window;

		public T selectedValue { get; private set; }
		public int Count => _values?.Length ?? 0;

		public GUIPopupSelector(T[] values, T selectedValue, Action<T, int> onSelected,
			Func<T, string> pathEvaluator = null, Func<T, string> nameEvaluator = null,
			Action onRefresh = null) :
			this(values, selectedValue, null, pathEvaluator, nameEvaluator, onRefresh, true, false)
		{
			_onSelectedIndex = onSelected;
		}

		public GUIPopupSelector(T[] values, T selectedValue, Action<T> onSelected,
			Func<T, string> pathEvaluator = null,
			Func<T, string> nameEvaluator = null,
			Action onRefresh = null,
			bool search = true,
			bool multiselect = false)
		{
			_values = values;
			_onSelected = onSelected;
			_pathEvaluator = pathEvaluator;
			_nameEvaluator = nameEvaluator;

			_enableSearch = search;
			_enableMultiselect = multiselect;
			_searchFunction = DefaultSearchFunction;

			if (onRefresh != null)
			{
				_functionButtonsInfo = new List<FunctionButtonInfo>
				{
					new FunctionButtonInfo {action = onRefresh, icon = EditorIcons.Refresh}
				};
			}

			this.selectedValue = ValidateSelectedValue(selectedValue) ? selectedValue : default;
		}

		public void Hide() => Hide(true);

		public void Hide(bool close)
		{
			if (close)
			{
				TryClose();
			}

			_show = false;
		}

		// Иконка пункта (например Sprite из config'а) — применяется при построении дерева
		public void SetIconEvaluator(Func<T, Sprite> evaluator)
		{
			_iconEvaluator = evaluator;
		}

		// Вторичная подпись (серым, rich text) рядом с именем пункта — например полное имя вложенного ассета
		public void SetSecondaryLabelEvaluator(Func<T, string> evaluator)
		{
			_secondaryLabelEvaluator = evaluator;
		}

		public void SetSearchFunction(Func<OdinMenuItem, bool> function)
		{
			_searchFunction = function;

			if (_tree != null)
			{
				_tree.Config.SearchFunction = function;
			}
		}

		public string GetSearchTerm()
		{
			return _tree?.Config.SearchTerm ?? string.Empty;
		}

		public bool Contains(T value)
		{
			return _values?.Contains(value) ?? false;
		}

		public void AddFunctionButtons(params FunctionButtonInfo[] infos)
		{
			if (infos.IsNullOrEmpty())
				return;

			_functionButtonsInfo ??= new List<FunctionButtonInfo>();

			for (int i = infos.Length; i-- > 0;)
			{
				_functionButtonsInfo.Add(infos[i]);
			}
		}

		public bool SetShow(bool value)
		{
			_show = value;
			return _show;
		}

		public bool DrawDropdown(Rect rect, bool drawFunctions = true)
		{
			return DrawDropdown(rect, rect, drawFunctions);
		}

		public bool DrawDropdown(Rect rect, Rect popupRect, bool drawFunctions = true)
		{
			var label = selectedValue != null
				? _nameEvaluator != null ? _nameEvaluator.Invoke(selectedValue) : selectedValue.ToString()
				: string.Empty;

			return DrawDropdown(rect, popupRect, new GUIContent(label), drawFunctions);
		}

		public bool DrawDropdown(Rect rect, Rect popupRect, GUIContent valueLabel, bool drawFunctions = true)
		{
			if (drawFunctions && !_functionButtonsInfo.IsNullOrEmpty())
			{
				rect.width -= (rect.height * _functionButtonsInfo.Count);
			}

			var show = DrawDropdown(rect, popupRect, valueLabel);

			if (drawFunctions)
			{
				DrawFunctionButtons(rect);
			}

			UpdateItemsNameOnSearch();

			return show;
		}

		private bool DrawDropdown(Rect rect, Rect popupRect, GUIContent valueLabel)
		{
			if (EditorGUI.DropdownButton(rect, valueLabel, FocusType.Passive))
			{
				ShowPopup(popupRect);
			}

			return _show;
		}

		public bool ShowPopup(Rect position)
		{
			_show = !_show;

			if (_show)
			{
				_window = ShowInPopup(position);
				_window.OnClose += OnClosed;
			}
			else
			{
				_window = null;
			}

			return _show;
		}

		// Явный размер окна: Unity корректно переворачивает dropdown вверх, если снизу не хватает места
		public bool ShowPopup(Rect position, Vector2 windowSize)
		{
			_show = !_show;

			if (_show)
			{
				_window = ShowInPopup(position, windowSize);
				_window.OnClose += OnClosed;
			}
			else
			{
				_window = null;
			}

			return _show;
		}

		private void DrawFunctionButtons(Rect position)
		{
			if (_functionButtonsInfo.IsNullOrEmpty())
				return;

			for (int i = _functionButtonsInfo.Count; i-- > 0;)
			{
				var nextInfo = _functionButtonsInfo[i];

				DrawFunctionButton(position, nextInfo);
				position.width += position.height;
			}
		}

		private void DrawFunctionButton(Rect position, FunctionButtonInfo info)
		{
			if (info.action == null)
				return;

			var size = position.height;

			position.x = position.xMax + 1;
			position.width = size;

			var clicked = info.icon != null
				? FusumityEditorGUILayout.ToolbarButton(position, info.icon, GUI.skin.button)
				: FusumityEditorGUILayout.ToolbarButton(position, info.sdfIcon, GUI.skin.button);

			if (clicked)
			{
				info.action?.Invoke();
			}
		}

		public void SelectFirst()
		{
			var first = _values.First();
			SetSelection(first);
		}

		public override void SetSelection(T selected)
		{
			if (_values.Contains(selected))
			{
				selectedValue = selected;
				base.SetSelection(selected);
			}
		}

		protected override void BuildSelectionTree(OdinMenuTree tree)
		{
			_tree = tree;
			tree.Config.UseCachedExpandedStates = false;

			tree.Config.DrawSearchToolbar = _enableSearch;
			tree.Selection.SupportsMultiSelect = _enableMultiselect;

			if (_searchFunction != null)
			{
				tree.Config.SearchFunction = _searchFunction;
			}

			if (_secondaryLabelEvaluator != null && tree.DefaultMenuStyle != null)
			{
				// Копии, а не мутация: по умолчанию DefaultLabelStyle возвращает общий SirenixGUIStyles.Label
				tree.DefaultMenuStyle.DefaultLabelStyle = new GUIStyle(tree.DefaultMenuStyle.DefaultLabelStyle) {richText = true};
				tree.DefaultMenuStyle.SelectedLabelStyle = new GUIStyle(tree.DefaultMenuStyle.SelectedLabelStyle) {richText = true};
			}

			for (int i = 0; i < _values.Length; i++)
			{
				var value = _values[i];

				string path = _pathEvaluator != null ? _pathEvaluator.Invoke(value) : value.ToString();

				OdinMenuItem leaf;
				var sprite = _iconEvaluator?.Invoke(value);
				if (sprite != null)
				{
					// IconGetter, а не Add(..., Sprite): AssetPreview.GetAssetPreview асинхронный и при
					// разовом построении дерева вернёт null — геттер же пересчитается, когда превью готово
					leaf = tree.Add(path, value).LastOrDefault();
					if (leaf != null)
						leaf.IconGetter = () =>
						{
							var preview = AssetPreview.GetAssetPreview(sprite);
							// Окно перерисовывается только по событиям — форсируем, пока превью догружаются
							if (preview == null && _window && AssetPreview.IsLoadingAssetPreviews())
								_window.Repaint();

							return preview;
						};
				}
				else if (value is UnityObject obj)
					leaf = tree.Add(path, value, EditorGUIUtility.GetIconForObject(obj)).LastOrDefault();
				else
					leaf = tree.Add(path, value).LastOrDefault();

				var secondary = _secondaryLabelEvaluator?.Invoke(value);
				if (leaf != null && !string.IsNullOrEmpty(secondary))
					leaf.Name += $"  <color=#808080>{secondary}</color>";
			}

			SetSelection(selectedValue);

			SelectionChanged += col =>
			{
				var value = col.FirstOrDefault();
				var index = _values.IndexOf(value);

				if (index != -1)
				{
					selectedValue = value;
					_onSelected?.Invoke(selectedValue);
					_onSelectedIndex?.Invoke(selectedValue, index);

					if (!Event.current.isKey)
						Hide(true);
				}
			};

			SelectionConfirmed += col =>
			{
				var value = col.FirstOrDefault();
				var index = _values.IndexOf(value);

				if (index != -1)
				{
					selectedValue = value;
					_onSelected?.Invoke(selectedValue);
					_onSelectedIndex?.Invoke(selectedValue, index);
				}
			};
		}

		private bool ValidateSelectedValue(T value)
		{
			if (value == null)
				return true;

			for (int i = 0; i < _values.Length; i++)
			{
				if (value.Equals(_values[i]))
					return true;
			}

			return false;
		}

		public bool TryGetMenuItemByValue(T value, out OdinMenuItem item)
		{
			item = null;

			if (_tree == null)
				return false;

			if (FindItemByValue(value, _tree.MenuItems, out item))
			{
				return true;
			}

			return false;
		}

		private bool FindItemByValue(T value, List<OdinMenuItem> list, out OdinMenuItem item)
		{
			item = null;

			foreach (var x in list)
			{
				if (x.Value != null && x.Value.Equals(value))
				{
					item = x;
					return true;
				}

				if (FindItemByValue(value, x.ChildMenuItems, out item))
				{
					return true;
				}
			}

			return false;
		}

		private void UpdateItemsNameOnSearch()
		{
			if (_window != null && !string.IsNullOrEmpty(GetSearchTerm()))
			{
				if (_cacheItemNames == null)
				{
					_cacheItemNames = new Dictionary<T, string>();

					foreach (var value in _values)
					{
						if (TryGetMenuItemByValue(value, out var item))
						{
							_cacheItemNames[value] = item.Name;

							item.Name = item.GetFullPath();
						}
					}
				}
			}
			else
			{
				if (_cacheItemNames != null)
				{
					foreach (var value in _values)
					{
						if (_cacheItemNames.TryGetValue(value, out var oldName))
						{
							if (TryGetMenuItemByValue(value, out var item))
							{
								item.Name = oldName;
							}
						}
					}

					_cacheItemNames = null;
				}
			}
		}

		public bool DefaultSearchFunction(OdinMenuItem item)
		{
			var value = (T) item.Value;

			if (!_values.Contains(value))
				return false;

			var rawSearchTerm = GetSearchTerm();

			if (rawSearchTerm.IsNullOrEmpty())
				return false;

			var searchTerm = rawSearchTerm.ToLower();
			var target = item.GetFullPath().ToLower();
			var contains = target.Contains(searchTerm);

			return contains;
		}

		protected override void DrawToolbar()
		{
			if (!(!string.IsNullOrEmpty(this.Title) | this.SelectionTree.Config.DrawSearchToolbar |
				    this.DrawConfirmSelectionButton))
				return;

			SirenixEditorGUI.BeginHorizontalToolbar((float) this.SelectionTree.Config.SearchToolbarHeight);
			DrawToolbarTitle();
			DrawToolbarSearch();
			EditorGUI.DrawRect(GUILayoutUtility.GetLastRect().AlignLeft(1f), SirenixGUIStyles.BorderColor);
			DrawFunctionButtonInToolbar(); //новая строчка
			DrawToolbarConfirmButton();
			SirenixEditorGUI.EndHorizontalToolbar();
		}

		public void AddToolbarFunctionButtons(params FunctionButtonInfo[] infos)
		{
			if (infos.IsNullOrEmpty())
				return;

			_toolbarFunctionButtonsInfo ??= new List<FunctionButtonInfo>();

			for (int i = infos.Length; i-- > 0;)
			{
				_toolbarFunctionButtonsInfo.Add(infos[i]);
			}
		}

		private void DrawFunctionButtonInToolbar()
		{
			if (!_toolbarFunctionButtonsInfo.IsNullOrEmpty())
				foreach (var info in _toolbarFunctionButtonsInfo)
				{
					var clicked = info.icon != null
						? SirenixEditorGUI.ToolbarButton(info.icon)
						: SirenixEditorGUI.ToolbarButton(info.sdfIcon);

					if (clicked)
						info.action?.Invoke();
				}
		}

		private void TryClose()
		{
			if (_window)
			{
				_window.Close();
				_window = null;
			}
		}

		private void OnClosed()
		{
			if (_window)
			{
				_window.OnClose -= OnClosed;
				_window = null;
			}

			Hide(false);
		}
	}

	public struct FunctionButtonInfo
	{
		public Action action;
		public EditorIcon icon;
		public SdfIconType sdfIcon;
	}
}
