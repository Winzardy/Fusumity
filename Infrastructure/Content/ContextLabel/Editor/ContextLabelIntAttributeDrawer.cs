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
		private const float SUFFIX_MARGIN = 16f;

		private int _cacheKeyCount = -1;
		private bool? _showedSelectorBeforeClick;
		private bool _expanded;

		private GUIPopupSelector<int> _selector;

		private UniqueContentEntry<ContextLabelCatalog<int>> _contentEntry;
		private ref readonly ContextLabelCatalog<int> currentCatalog => ref _contentEntry.Value;

		private ScriptableObject Asset => _contentEntry is IScriptableContentEntry scriptableObjectEntry
			? scriptableObjectEntry.ScriptableObject
			: null;

		protected override void Initialize()
		{
			TryResolveEntry();
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (_contentEntry == null)
			{
				TryResolveEntry();
				if (_contentEntry == null)
				{
					if (!ContentManager.initializing)
						SirenixEditorGUI.WarningMessageBox($"Not found catalog (int) by id [ {Attribute.Catalog} ] ");
					CallNextDrawer(label);
					return;
				}
			}

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

			string labelByKey = null;
			var hasLabel = currentCatalog.TryGet(selectedKey, out labelByKey);

			// Если лейбл каталога не влезает рядом с полем — вместо наплыва текста прячем его
			// в разворачивающийся блок, как переполнение перевода в LocKeyAttributeDrawer
			var overflow = hasLabel && !FitsSuffix(textFieldPosition, label, selectedKey, labelByKey, style);

			if (overflow)
			{
				var foldoutRect = textFieldPosition;
				foldoutRect.width = SirenixEditorGUI.FoldoutWidth;
				_expanded = SirenixEditorGUI.Foldout(foldoutRect, _expanded, GUIContent.none);

				textFieldPosition.xMin += SirenixEditorGUI.FoldoutWidth;
			}
			else
			{
				_expanded = false;
			}

			selectedKey = ValueEntry.SmartValue = SirenixEditorFields.IntField(textFieldPosition, label, selectedKey, style);
			GUI.color = originalColor;

			if (hasLabel)
			{
				if (overflow)
				{
					if (SirenixEditorGUI.BeginFadeGroup(this, _expanded))
					{
						using (new GUILayout.HorizontalScope())
						{
							var textAreaStyle = new GUIStyle(GUI.skin.textArea);
							var padding = textAreaStyle.padding;
							padding.left += 3;
							padding.top += 3;
							padding.bottom = 4;
							textAreaStyle.padding = padding;

							GUILayout.TextArea(labelByKey, textAreaStyle);
							var textAreaRect = GUILayoutUtility.GetLastRect();

							EditorGUIUtility.AddCursorRect(textAreaRect, MouseCursor.Text);
						}
					}

					SirenixEditorGUI.EndFadeGroup();
				}
				else
				{
					// Рект явно: под [ValueDropdown]/[InlineProperty] поле живёт с отступом,
					// и суффикс должен считаться от него же
					FusumityEditorGUILayout.SuffixValue(textFieldPosition, label, selectedKey, labelByKey, style,
						EditorStyles.label);
				}
			}

			if (!_selector.show)
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretDownFill);
			else
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretUpFill);
		}

		// Насколько бы влез лейбл каталога рядом со значением поля (как в SuffixValue), без реальной отрисовки
		private static bool FitsSuffix(Rect fieldRect, GUIContent label, int value, string suffixText, GUIStyle valueStyle)
		{
			var labelWidth = label.text.IsNullOrEmpty() ? 0f : EditorGUIUtility.labelWidth;
			var available = fieldRect.width - labelWidth;
			var occupied = valueStyle.CalcWidth(value.ToString()) + EditorStyles.miniLabel.CalcWidth(suffixText) + SUFFIX_MARGIN;
			return occupied <= available;
		}

		private void TryResolveEntry()
		{
			if (ContentManager.initializing)
				return;

			ContentManager.TryGetEntry(Attribute.Catalog, out _contentEntry);
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

			var selector = new GUIPopupSelector<int>(keys.ToArray(),
				ValueEntry.SmartValue + 1,
				HandleSelected,
				pathEvaluator: key =>
				{
					if (key == NONE_KEY)
						return NONE_MENU;

					var i = key - 1;
					return currentCatalog[i];
				});

			selector.SetSearchFunction(item =>
			{
				if (item.GetFullPath() == NONE_MENU)
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

			selector.AddToolbarFunctionButtons(new FunctionButtonInfo
			{
				action = PromptAddNew, sdfIcon = SdfIconType.Plus
			});

			return selector;
		}

		private void SelectAsset()
		{
			EditorGUIUtility.PingObject(Asset);
		}

		private void PromptAddNew()
		{
			_selector.SetSelection(ValueEntry.SmartValue + 1);
			if (Asset != null)
				GUIHelper.OpenInspectorWindow(Asset);
			_selector?.Hide();
		}

		private void HandleSelected(int key)
		{
			if (key == NONE_KEY)
				return;

			ValueEntry.WeakSmartValue = key - 1;
		}
	}
}
