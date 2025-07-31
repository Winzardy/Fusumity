using Content.ScriptableObjects;
using Fusumity.Editor;
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
	public class ContextLabelIntAttributeDrawer : OdinAttributeDrawer<ContextLabelAttribute, int>
	{
		private const string NONE_MENU = "None";
		private const int NONE_KEY = 0;

		private const string ADD_MENU = "Add new";
		private const int ADD_KEY = -1;

		private int _cacheKeyCount = -1;
		private bool? _showedSelectorBeforeClick;

		private GUIPopupSelector<int> _selector;

		private IContentEntry<ContextLabelCatalog<int>> _contentEntry;
		private ref readonly ContextLabelCatalog<int> currentCatalog => ref _contentEntry.Value;

		private ScriptableObject Asset => _contentEntry is IScriptableContentEntry scriptableObjectEntry
			? scriptableObjectEntry.ScriptableObject
			: null;

		protected override void Initialize()
		{
			_contentEntry = ContentManager.GetEntry<ContextLabelCatalog<int>>(Attribute.Catalog);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			TryCreateSelector();

			var selectedKey = ValueEntry.SmartValue;
			if (_selector == null)
			{
				ValueEntry.SmartValue = SirenixEditorFields.IntField(label, selectedKey);
				return;
			}

			if (_selector == null)
				return;

			var contains = currentCatalog.Contains(selectedKey);
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

			selectedKey = ValueEntry.SmartValue = SirenixEditorFields.IntField(textFieldPosition, label, selectedKey, style);
			GUI.color = originalColor;

			if (currentCatalog.TryGet(selectedKey, out var labelByKey))
				FusumityEditorGUILayout.SuffixValue(label, selectedKey, labelByKey, EditorStyles.label, -1.5f);

			if (!_selector.show)
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretDownFill);
			else
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretUpFill);

			CustomizeAddMenu();
		}

		private void CustomizeAddMenu()
		{
			if (!_selector.TryGetMenuItemByValue(ADD_KEY, out var menuItem))
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

		private GUIPopupSelector<int> CreateSelector(in ContextLabelCatalog<int> catalog)
		{
			_cacheKeyCount = catalog.Count;
			using var _ = ListPool<int>.Get(out var keys);
			foreach (var key in catalog.GetKeys())
				keys.Add(key + 1);
			keys.Add(ADD_KEY);
			var selector = new GUIPopupSelector<int>(keys.ToArray(),
				ValueEntry.SmartValue + 1,
				HandleSelected,
				pathEvaluator: key =>
				{
					if (key == NONE_KEY)
						return NONE_MENU;

					if (key == ADD_KEY)
						return ADD_MENU;

					var i = key - 1;
					return currentCatalog[i];
				});

			selector.SetSearchFunction(item =>
			{
				if (item.GetFullPath() == ADD_MENU ||
				    item.GetFullPath() == NONE_MENU)
					return false;

				if (item?.Value == null)
					return false;

				var key = (int) item.Value;
				var s = currentCatalog[key - 1].ToLower();
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

		private void HandleSelected(int key)
		{
			if (key == NONE_KEY)
				return;

			if (key == ADD_KEY)
			{
				_selector.SetSelection(ValueEntry.SmartValue + 1);
				if (Asset != null)
					GUIHelper.OpenInspectorWindow(Asset);
				_selector?.Hide();
				return;
			}

			ValueEntry.WeakSmartValue = key - 1;
		}
	}
}
