using System.Collections.Generic;
using System.Globalization;
using Fusumity.Editor;
using Fusumity.Editor.Utility;
using Fusumity.Utility;
using I2.Loc;
using JetBrains.Annotations;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Loc = I2.Loc.LocalizationManager;

namespace Localizations.Editor
{
	public class LocKeyAttributeDrawer : OdinAttributeDrawer<LocKeyAttribute, string>
	{
		private const string INDENT_SPACE = "    ";

		private static readonly string LABEL = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName == "ru"
			? "Перевод"
			: "Translation";

		private static readonly string TOOLTIP_PREFIX = $"{LABEL}:\n".ColorText(Color.gray).SizeText(12);
		private static readonly string TEXT_AREA_LABEL = $"{LABEL}:";

		private const int MAX_TRANSLATE_LENGHT = 32;

		private List<string> allKeys;
		private GUIPopupSelector<string> _selector;

		private bool _hideDetailedMessage;

		private bool? _showedSelectorBeforeClick;

		private string _language;

		private bool _textAreaRichText = true;

		protected override void Initialize()
		{
			allKeys = Localization.GetAllKeysEditor();
			var selectedKey = ValueEntry.SmartValue;
			CreateSelector(selectedKey);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var selectedKey = ValueEntry.SmartValue;

			if (string.IsNullOrEmpty(selectedKey))
				selectedKey = string.Empty;

			var contains = allKeys.Contains(selectedKey);

			if (Loc.GetTermsList().Count != allKeys.Count)
			{
				allKeys = Loc.GetTermsList();
				CreateSelector(selectedKey);
			}
			else
			{
				if (_selector.selectedValue != selectedKey)
					_selector.SetSelection(selectedKey);
			}

			var fieldName = Attribute.FieldName;
			var valueByFieldName = Property.ParentValueProperty.ValueEntry.WeakSmartValue.GetReflectionValue(fieldName);
			_language = valueByFieldName is string x ? x : null;

			label ??= new GUIContent();

			EditorGUILayout.GetControlRect();

			var translation = Localization.GetEditor(selectedKey, _language) ?? string.Empty;

			var rect = GUILayoutUtility.GetLastRect();

			var selectorPopupRect = rect;
			var textFieldPosition = rect;
			var trianglePosition = AlignRight(rect, 9f, 5f);

			EditorGUIUtility.AddCursorRect(trianglePosition, MouseCursor.Arrow);

			var labelWidth = string.IsNullOrEmpty(label.text) ? 0f : EditorGUIUtility.labelWidth;
			var full = rect.width;
			var width = full - labelWidth;
			var shortRectWidth = width;
			shortRectWidth /= 2;

			var o = 16f;
			var shortTranslatePosition = AlignRight(rect, shortRectWidth, o);

			if (trianglePosition.Contains(Event.current.mousePosition))
				_showedSelectorBeforeClick ??= _selector.show;

			if (GUI.Button(trianglePosition, GUIContent.none, GUIStyle.none))
			{
				var click = !_showedSelectorBeforeClick ?? true;
				if (click)
					_selector.ShowPopup(selectorPopupRect);

				_showedSelectorBeforeClick = null;
			}

			var originalColor = GUI.color;
			var canBeEmpty = Property.GetAttribute<CanBeNullAttribute>() != null;
			GUI.color = contains
				? originalColor
				: !canBeEmpty
					? SirenixGUIStyles.YellowWarningColor
					: originalColor;

			var originalTooltip = label.tooltip;
			if (contains)
			{
				if (!translation.IsNullOrEmpty())
				{
					if (!string.IsNullOrEmpty(label.tooltip))
						label.tooltip += "\n";

					label.tooltip += $"{TOOLTIP_PREFIX}{translation}";
				}
			}

			var maxLenght = MAX_TRANSLATE_LENGHT;

			var style = new GUIStyle(EditorStyles.miniLabel)
			{
				normal =
				{
					textColor = Color.gray
				},
				hover =
				{
					textColor = Color.gray
				}
			};
			style.padding.top += 3;
			style.alignment = TextAnchor.UpperRight;
			var shortLabelSize = style.CalcSize(translation);
			var labelSize = GUI.skin.textField.CalcSize(selectedKey);
			var sum = shortLabelSize.x + labelSize.x + o + 5f;

			var useDetailed = translation.Length > maxLenght || sum >= width;
			if (useDetailed)
			{
				var foldoutRect = textFieldPosition;
				foldoutRect.width = SirenixEditorGUI.FoldoutWidth;

				_hideDetailedMessage = SirenixEditorGUI.Foldout(foldoutRect, _hideDetailedMessage, "");

				if (SirenixEditorGUI.BeginFadeGroup(this, _hideDetailedMessage))
				{
					using (new GUILayout.HorizontalScope())
					{
						const float SIZE = 12f;
						const float MARGIN_RIGHT = -3f;
						const float MARGIN_TOP = 23.5f;

						var originalEnable = GUI.enabled;
						var isRichText = translation.IsRichText();

						var buttonRect = trianglePosition;

						buttonRect.y += MARGIN_TOP;
						buttonRect.x += MARGIN_RIGHT;
						buttonRect.width = SIZE;
						buttonRect.height = SIZE;

						if (!_textAreaRichText)
						{
							buttonRect.y += 1;
							buttonRect.x += 0.5f;
							buttonRect.width -= 0.5f;
							buttonRect.height -= 0.5f;
						}

						if (isRichText)
						{
							if (GUI.Button(buttonRect, GUIContent.none))
								_textAreaRichText = !_textAreaRichText;
							EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Arrow);
						}

						//GUI.enabled = false;
						var textAreaStyle = new GUIStyle(GUI.skin.textArea);
						var padding = textAreaStyle.padding;
						padding.top = 3 + 14;
						padding.left += 3;
						padding.bottom = 4;
						textAreaStyle.padding = padding;
						textAreaStyle.richText = _textAreaRichText;

						GUILayout.TextArea(translation, textAreaStyle);
						var textAreaRect = GUILayoutUtility.GetLastRect();

						EditorGUIUtility.AddCursorRect(textAreaRect, MouseCursor.Text);

						//Label
						var labelStyle = new GUIStyle(GUI.skin.label)
						{
							fontSize = 12,
							richText = true
						};

						labelStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
						labelStyle.hover.textColor = new Color(0.5f, 0.5f, 0.5f);
						var labelRect = textAreaRect.AlignLeft(textAreaRect.width - 54).AlignTop(15).Padding(3, 0, 0, 0);
						labelRect.y += 2;
						GUI.Label(labelRect, TEXT_AREA_LABEL, labelStyle);

						if (isRichText)
						{
							//Toggle
							var buttonStyle = new GUIStyle(SirenixGUIStyles.IconButton);
							buttonStyle.fontSize = 9;
							var originColor = GUI.color;
							GUI.color = !_textAreaRichText ? originColor : originalColor.WithAlpha(0.25f);

							GUI.enabled = true;

							SdfIcons.DrawIcon(buttonRect, _textAreaRichText ? SdfIconType.EyeFill : SdfIconType.EyeSlashFill);

							GUI.color = originColor;
						}

						GUI.enabled = originalEnable;
					}
				}

				SirenixEditorGUI.EndFadeGroup();

				if (!EditorGUIUtility.hierarchyMode)
				{
					if (!label.text.Contains(INDENT_SPACE))
						label.text = INDENT_SPACE + label.text;

					// var offset = SirenixEditorGUI.FoldoutWidth + 3;
					// textFieldPosition.x += offset;
					// textFieldPosition.width -= offset;
				}
			}
			else
			{
				_hideDetailedMessage = false;
			}

			ValueEntry.SmartValue = SirenixEditorFields.TextField(textFieldPosition, label, selectedKey);

			var shortTranslateLabel = translation;

			if (useDetailed && shortTranslateLabel.Length > 3)
			{
				shortTranslateLabel = $"{translation.Substring(0, maxLenght.Min(shortTranslateLabel.Length) - 3)}...  ";
				style.normal.textColor *= 0.5f;
				style.hover.textColor *= 0.5f;
			}

			GUI.Label(shortTranslatePosition, shortTranslateLabel, style);

			label.tooltip = originalTooltip;
			GUI.color = originalColor;

			SdfIcons.DrawIcon(trianglePosition, !_selector.show ? SdfIconType.CaretDownFill : SdfIconType.CaretUpFill);
		}

		private void CreateSelector(string selectedKey)
		{
			_selector = new GUIPopupSelector<string>
			(
				allKeys.ToArray(),
				selectedKey,
				OnSelected,
				pathEvaluator: PathEvaluator
			);
			_selector.AddToolbarFunctionButtons(new FunctionButtonInfo
			{
				action = SelectAsset, icon = EditorIcons.List
			});
		}

		private Rect AlignRight(Rect rect, float width, float offset = 0)
		{
			rect.x = rect.x + rect.width - width - offset;
			rect.width = width;
			return rect;
		}

		private void SelectAsset()
		{
			var asset = AssetDatabaseUtility.GetAsset<LanguageSourceAsset>();
			EditorGUIUtility.PingObject(asset);
		}

		private string PathEvaluator(string key)
		{
			var translate = key.IsNullOrWhitespace() ? string.Empty : Localization.GetEditor(key, _language);

			if (!translate.IsNullOrWhiteSpace())
			{
				translate = translate.Replace("\\", string.Empty)
				   .Replace("/", string.Empty);
			}

			var path = key; //.Replace("_", "/");
			return $"{path} ({translate})";
		}

		private void OnSelected(string key)
		{
			ValueEntry.WeakSmartValue = key;
		}
	}
}
