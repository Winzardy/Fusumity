using System.Collections.Generic;
using System.Linq;
using Fusumity.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Internal;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace AssetManagement.Editor
{
	public class ResourceSelectorPropertyDrawer : OdinAttributeDrawer<ResourceSelectorAttribute, string>
	{
		private const int MAX_TRANSLATE_LENGHT = 32;

		private IEnumerable<Object> allObjectsByType;
		private GUIPopupSelector<Object> _selector;

		private bool _hideDetailedMessage;

		private bool? _showedSelectorBeforeClick;

		protected override void Initialize()
		{
			allObjectsByType = GetKeys();

			Object selectedObj = null;
			foreach (var obj in allObjectsByType)
			{
				if (GetResourcePath(obj) != ValueEntry.SmartValue)
					continue;

				selectedObj = obj;
				break;
			}

			CreateSelector(selectedObj);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			Object selectedObj = null;
			foreach (var obj in allObjectsByType)
			{
				if (GetResourcePath(obj) != ValueEntry.SmartValue)
					continue;

				selectedObj = obj;
				break;
			}

			if (GetKeys().Count() != allObjectsByType.Count())
			{
				allObjectsByType = GetKeys();
				CreateSelector(selectedObj);
			}
			else
			{
				if (_selector.selectedValue != selectedObj)
					_selector.SetSelection(selectedObj);
			}

			label ??= new GUIContent();

			EditorGUILayout.GetControlRect();

			var rect = GUILayoutUtility.GetLastRect();

			var selectorPopupRect = rect;
			var textFieldPosition = rect;
			var trianglePosition = AlignRight(rect, 9f, selectedObj ? 20f : 5f);
			var pingPosition = AlignRight(trianglePosition, 2f, -5f);
			pingPosition.y += 3.5f;
			pingPosition.width = pingPosition.height = 12;
			if (trianglePosition.Contains(Event.current.mousePosition))
				_showedSelectorBeforeClick ??= _selector.show;
			if (GUI.Button(trianglePosition, GUIContent.none, GUIStyle.none))
			{
				var click = !_showedSelectorBeforeClick ?? true;
				if (click)
					_selector.ShowPopup(selectorPopupRect);

				_showedSelectorBeforeClick = null;
			}

			EditorGUIUtility.AddCursorRect(trianglePosition, MouseCursor.Arrow);

			if (selectedObj && GUI.Button(pingPosition, GUIContent.none, GUIStyle.none))
			{
				EditorGUIUtility.PingObject(selectedObj);
			}
			EditorGUIUtility.AddCursorRect(pingPosition, MouseCursor.Arrow);

			var originalColor = GUI.color;
			GUI.color = selectedObj ? originalColor : SirenixGUIStyles.YellowWarningColor;

			var originalTooltip = label.tooltip;

			var objPath = GetResourcePath(selectedObj);
			ValueEntry.SmartValue =
				SirenixEditorFields.TextField(textFieldPosition, label,
					selectedObj ? objPath : ValueEntry.SmartValue);

			var style = new GUIStyle(EditorStyles.miniLabel);
			style.normal.textColor = Color.gray;
			style.hover.textColor = Color.gray;
			style.alignment = TextAnchor.MiddleRight;

			label.tooltip = originalTooltip;
			GUI.color = originalColor;

			if (!_selector.show)
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretDownFill);
			else
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretUpFill);

			if (selectedObj)
			{
				GUITextureDrawingUtil.DrawTexture(
					pingPosition,
					EditorIcons.ObjectFieldButton,
					ScaleMode.ScaleToFit, Color.white,
					Color.clear);
			}
		}

		private void CreateSelector(Object selected)
		{
			_selector = new GUIPopupSelector<Object>
			(
				allObjectsByType.ToArray(),
				selected,
				OnSelected,
				pathEvaluator: GetResourcePath
			);
		}

		private string GetResourcePath(Object obj)
		{
			if (obj)
			{
				var path = EditorResources.GetAssetPath(obj);
				var split = path.Split("Resources/");
				var result = split[1].Split(".");
				return result[0];
			}

			return string.Empty;
		}

		private IEnumerable<Object> GetKeys() => Resources.FindObjectsOfTypeAll(Attribute.Type);

		private Rect AlignRight(Rect rect, float width, float offset = 0)
		{
			rect.x = rect.x + rect.width - width - offset;
			rect.width = width;
			return rect;
		}

		private void OnSelected(Object key)
		{
			if (key)
				ValueEntry.WeakSmartValue = GetResourcePath(key);
		}
	}
}
