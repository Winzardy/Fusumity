using System.Collections.Generic;
using System.Linq;
using Fusumity.Editor;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Internal;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

namespace Localization.Editor
{
	public class LocTableReferencePropertyDrawer : OdinValueDrawer<LocTableReference>
	{
		private const int MAX_TRANSLATE_LENGHT = 32;

		private IEnumerable<string> allKeys;
		private GUIPopupSelector<string> _selector;

		private bool _hideDetailedMessage;

		private bool? _showedSelectorBeforeClick;

		protected override void Initialize()
		{
			allKeys = GetKeys();

			CreateSelector(ValueEntry.SmartValue);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			string selectedKey = ValueEntry.SmartValue;

			var isEmpty = selectedKey.IsNullOrEmpty();
			var contains = allKeys.Contains(selectedKey);

			if (GetKeys().Count() != allKeys.Count())
			{
				allKeys = GetKeys();
				CreateSelector(selectedKey);
			}
			else
			{
				if (_selector.selectedValue != selectedKey)
					_selector.SetSelection(selectedKey);
			}

			label ??= new GUIContent();

			EditorGUILayout.GetControlRect();

			var rect = GUILayoutUtility.GetLastRect();

			var selectorPopupRect = rect;
			var textFieldPosition = rect;
			var trianglePosition = AlignRight(rect, 9f, contains ? 20f : 5f);
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

			if (contains && GUI.Button(pingPosition, GUIContent.none, GUIStyle.none))
			{
				var collection = LocalizationEditorSettings.GetStringTableCollection(selectedKey);
				EditorGUIUtility.PingObject(collection);
			}

			EditorGUIUtility.AddCursorRect(pingPosition, MouseCursor.Arrow);

			var originalColor = GUI.color;
			GUI.color = contains || isEmpty ? originalColor : SirenixGUIStyles.YellowWarningColor;

			var originalTooltip = label.tooltip;

			ValueEntry.SmartValue = SirenixEditorFields.TextField(textFieldPosition, label, ValueEntry.SmartValue);

			var style = new GUIStyle(EditorStyles.miniLabel)
			{
				normal =
				{
					textColor = Color.gray
				},
				hover =
				{
					textColor = Color.gray
				},
				alignment = TextAnchor.MiddleRight
			};

			label.tooltip = originalTooltip;
			GUI.color = originalColor;

			if (!_selector.show)
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretDownFill);
			else
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretUpFill);

			if (contains)
			{
				GUITextureDrawingUtil.DrawTexture(
					pingPosition,
					EditorIcons.ObjectFieldButton,
					ScaleMode.ScaleToFit, Color.white,
					Color.clear);
			}
		}

		private void CreateSelector(string selected)
		{
			_selector = new GUIPopupSelector<string>
			(
				allKeys.ToArray(),
				selected,
				OnSelected,
				pathEvaluator: key => key.ToString()
			);
		}

		private IEnumerable<string> GetKeys()
		{
			return LocalizationEditorSettings.GetStringTableCollections()
			   .Select(x => x.TableCollectionName);
		}

		private Rect AlignRight(Rect rect, float width, float offset = 0)
		{
			rect.x = rect.x + rect.width - width - offset;
			rect.width = width;
			return rect;
		}

		private void OnSelected(string key)
		{
			var ltr = ValueEntry.SmartValue;
			ltr.id = key;
			ValueEntry.SmartValue = ltr;
		}
	}
	// public class LocTableReferenceAttributeProcessor : OdinAttributeProcessor<LocTableReference>
	// {
	// 	public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
	// 	{
	// 		base.ProcessChildMemberAttributes(parentProperty, member, attributes);
	//
	// 		switch (member.Name)
	// 		{
	// 			case "m_TableCollectionName":
	//
	// 				var attribute = new CustomValueDrawerAttribute(
	// 					$"@{nameof(LocTableReferenceAttributeProcessor)}.{nameof(Dropdown)}($value, $label)");
	// 				//attributes.Add(attribute);
	//
	// 				var label = parentProperty.Label.text;
	//
	// 				if (label.IsNullOrEmpty())
	// 					attributes.Add(new HideLabelAttribute());
	// 				else
	// 					attributes.Add(new LabelTextAttribute(label, true));
	//
	// 				break;
	// 		}
	// 	}
	//
	// 	public static string Dropdown(string current, GUIContent label)
	// 	{
	// 		var list = LocalizationEditorSettings.GetStringTableCollections()
	// 		   .Select(x => x.TableCollectionName)
	// 		   .ToList();
	// 		var names = list.Select(c => c.ToString())
	// 		   .ToArray();
	// 		var index = Mathf.Max(0, list.IndexOf(current));
	// 		var selected = EditorGUILayout.Popup(label.text, index, names);
	// 		return list[selected];
	// 	}
	// }
}
