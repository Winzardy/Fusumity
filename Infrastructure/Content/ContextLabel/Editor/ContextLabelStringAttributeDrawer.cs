using Content.ScriptableObjects;
using Fusumity.Editor;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.ContextLabel.Editor
{
	[CustomPropertyDrawer(typeof(ContextLabelAttribute))]
	public class ContextLabelStringAttributeDrawer : OdinAttributeDrawer<ContextLabelAttribute, string>
	{
		private const string NONE_MENU = "None";

		private const string ADD_MENU = "Add new";

		private int _cacheKeyCount = -1;
		private bool? _showedSelectorBeforeClick;

		private GUIPopupSelector<string> _selector;

		private UniqueContentEntry<ContextLabelCatalog<string>> _contentEntry;
		private ref readonly ContextLabelCatalog<string> currentCatalog => ref _contentEntry.Value;

		private ScriptableObject Asset => _contentEntry is IScriptableContentEntry scriptableObjectEntry
			? scriptableObjectEntry.ScriptableObject
			: null;

		protected override void Initialize()
		{
			ContentManager.TryGetEntry(Attribute.Catalog, out _contentEntry);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (_contentEntry == null)
			{
				SirenixEditorGUI.WarningMessageBox($"Not found catalog (string) by id [ {Attribute.Catalog} ] ");
				CallNextDrawer(label);
				return;
			}

			TryCreateSelector();

			var selectedKey = ValueEntry.SmartValue;
			if (_selector == null)
			{
				ValueEntry.SmartValue = SirenixEditorFields.TextField(label, selectedKey);
				return;
			}

			if (_selector == null)
				return;

			var contains = !selectedKey.IsNullOrEmpty() && currentCatalog.Contains(selectedKey);
			label ??= new GUIContent();
			EditorGUILayout.GetControlRect();

			var rect = GUILayoutUtility.GetLastRect();

			var selectorPopupRect = rect;
			var textFieldPosition = rect;
			var trianglePosition = rect.AlignRight(9f, 5f);

			if (trianglePosition.Contains(Event.current.mousePosition))
			{
				_showedSelectorBeforeClick ??= _selector.show;
			}

			if (GUI.Button(trianglePosition, GUIContent.none, GUIStyle.none))
			{
				var click = !_showedSelectorBeforeClick ?? true;
				if (click)
					_selector.ShowPopup(selectorPopupRect);

				_showedSelectorBeforeClick = null;
			}

			EditorGUIUtility.AddCursorRect(trianglePosition, MouseCursor.Arrow);

			var originalColor = GUI.color;

			var style = EditorStyles.textField;
			if (!contains)
				GUI.color = SirenixGUIStyles.YellowWarningColor;
			else
			{
				style = new GUIStyle(EditorStyles.textField)
				{
					fontSize = EditorStyles.textField.fontSize - 3,
					normal =
					{
						textColor = Color.gray
					},
					hover =
					{
						textColor = Color.gray
					}
				};
			}

			selectedKey = ValueEntry.SmartValue = SirenixEditorFields.TextField(textFieldPosition, label, selectedKey, style);
			GUI.color = originalColor;

			if (currentCatalog.TryGet(selectedKey, out var labelByKey))
				FusumityEditorGUILayout.SuffixValue(label, selectedKey, labelByKey, style,EditorStyles.label);

			if (!_selector.show)
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretDownFill);
			else
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretUpFill);

			CustomizeAddMenu();
		}

		private void CustomizeAddMenu()
		{
			if (!_selector.TryGetMenuItemByValue(ADD_MENU, out var menuItem))
				return;

			menuItem.Icon = EditorIcons.Plus.Active;
			var menuStyle = menuItem.Style;
			menuItem.Style = new OdinMenuStyle
			{
				DefaultLabelStyle = new GUIStyle(menuStyle.DefaultLabelStyle),
				SelectedLabelStyle = new GUIStyle(menuStyle.SelectedLabelStyle)
			};
			menuItem.Style.DefaultLabelStyle.normal.textColor = Color.grey;
			menuItem.Style.SelectedInactiveColorLightSkin = Color.gray;
		}

		private void TryCreateSelector()
		{
			if (_contentEntry == null)
				return;

			if (_selector != null && _cacheKeyCount == currentCatalog.Count)
				return;

			_selector = CreateSelector(in currentCatalog);
		}

		private GUIPopupSelector<string> CreateSelector(in ContextLabelCatalog<string> catalog)
		{
			_cacheKeyCount = catalog.Count;
			using var _ = ListPool<string>.Get(out var keys);
			foreach (var key in catalog.GetKeys())
				keys.Add(key);
			keys.Add(ADD_MENU);
			var selector = new GUIPopupSelector<string>(keys.ToArray(),
				ValueEntry.SmartValue,
				HandleSelected,
				pathEvaluator: key =>
				{
					if (key == NONE_MENU)
						return NONE_MENU;

					if (key == ADD_MENU)
						return ADD_MENU;

					return currentCatalog[key];
				});

			selector.SetSearchFunction(item =>
			{
				if (item.GetFullPath() == ADD_MENU ||
				    item.GetFullPath() == NONE_MENU)
					return false;

				if (item?.Value == null)
					return false;

				var key = (string) item.Value;
				var s = currentCatalog[key].ToLower();
				if (s.Contains(selector.GetSearchTerm().ToLower()))
					return true;
				return false;
			});

			selector.AddToolbarFunctionButtons(new FunctionButtonInfo
			{
				action = SelectAsset, icon = EditorIcons.List
			});

			return selector;
		}

		private void SelectAsset()
		{
			EditorGUIUtility.PingObject(Asset);
		}

		private void HandleSelected(string key)
		{
			if (key == NONE_MENU)
				return;

			if (key == ADD_MENU)
			{
				_selector.SetSelection(ValueEntry.SmartValue);
				if (Asset != null)
					GUIHelper.OpenInspectorWindow(Asset);
				_selector?.Hide();
				return;
			}

			ValueEntry.WeakSmartValue = key;
		}
	}
}
