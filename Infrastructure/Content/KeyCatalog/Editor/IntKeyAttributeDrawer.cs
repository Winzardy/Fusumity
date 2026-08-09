using Content.ScriptableObjects;
using Fusumity.Editor;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Keys.Editor
{
	[CustomPropertyDrawer(typeof(KeyAttribute))]
	public class IntKeyAttributeDrawer : OdinAttributeDrawer<KeyAttribute, int>
	{
		private const string NONE_MENU = "None";
		private const int NONE_KEY = 0;

		private int _cacheKeyCount = -1;
		private bool? _showedSelectorBeforeClick;
		private bool _expanded;

		private bool _isDictionaryKey;

		// Запас на переключение «влезает/не влезает»: у самого порога решение иначе щёлкает
		// от любого изменения ширины строки (например, когда появляется скроллбар), и поле
		// дёргается на ширину фолдаута — заодно за компанию с соседними такими же
		private const float OVERFLOW_HYSTERESIS = 16f;

		// Переполнение решается на Layout и держится весь кадр, ширина строки — замер прошлого
		// не-Layout прохода: иначе состав layout-контролов расходится между Layout и Repaint
		private bool _overflow;
		private float _rowWidth;

		private GUIPopupSelector<int> _selector;

		private UniqueContentEntry<KeyCatalog<int>> _contentEntry;
		private ref readonly KeyCatalog<int> currentCatalog => ref _contentEntry.Value;

		private ScriptableObject Asset => _contentEntry is IScriptableContentEntry scriptableObjectEntry
			? scriptableObjectEntry.ScriptableObject
			: null;

		protected override void Initialize()
		{
			TryResolveEntry();
			_isDictionaryKey = IsDictionaryKey();
		}

		/// <summary>
		/// Поле — ключ строки словаря (EditableKeyValuePair): смена ключа заставляет Odin
		/// пересобрать строку, поэтому писать значение на каждый символ нельзя
		/// </summary>
		private bool IsDictionaryKey()
		{
			for (var current = Property; current?.Parent != null; current = current.Parent)
			{
				var parentType = current.Parent.ValueEntry?.TypeOfValue;

				if (parentType == null || !parentType.IsGenericType ||
					parentType.GetGenericTypeDefinition() != typeof(EditableKeyValuePair<,>))
					continue;

				return current.Name == "Key";
			}

			return false;
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (_contentEntry == null)
			{
				TryResolveEntry();
				if (_contentEntry == null)
				{
					if (!ContentManager.initializing)
						KeyCatalogCreator.DrawMissingCatalogBox(Attribute.CatalogId, "int",
							typeof(KeyCatalog<int>), TryResolveEntry);
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

			// Заглушечный рект 1x1 приходит не только на Layout: у временных пар словаря ({temp})
			// и в первых кадрах попапов ширина тоже единица. По такой мерке «влезает/не влезает»
			// металось кадр за кадром — берём только осмысленную ширину
			if (textFieldPosition.width > 1f)
				_rowWidth = textFieldPosition.width;

			// Если лейбл каталога не влезает рядом с полем — вместо наплыва текста прячем его
			// в разворачивающийся блок, как переполнение перевода в LocKeyAttributeDrawer.
			// Решаем один раз, на Layout: от этого зависит состав контролов, а он обязан
			// совпадать у Layout и Repaint
			if (Event.current.type == EventType.Layout)
			{
				var probeRect = textFieldPosition;
				probeRect.width = _overflow
					? _rowWidth - OVERFLOW_HYSTERESIS
					: _rowWidth + OVERFLOW_HYSTERESIS;

				_overflow = hasLabel && _rowWidth > 0f &&
					!KeyDrawerGUI.FitsSuffix(probeRect, label, selectedKey.ToString(), labelByKey, style);
			}

			var overflow = _overflow;

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

			// Ключ словаря коммитим по Enter/расфокусу, а не на каждый символ: на смену ключа
			// Odin пересобирает строку словаря вместе с дровером — фокус слетает
			var editedKey = _isDictionaryKey
				? EditorGUI.DelayedIntField(textFieldPosition, label, selectedKey, style)
				: SirenixEditorFields.IntField(textFieldPosition, label, selectedKey, style);

			if (editedKey != selectedKey)
				ValueEntry.SmartValue = editedKey;

			selectedKey = editedKey;
			GUI.color = originalColor;

			if (hasLabel && !overflow)
				KeyDrawerGUI.DrawSuffix(textFieldPosition, label, selectedKey.ToString(), labelByKey, style);

			// Fade-группу открываем всегда, даже свёрнутой: под условием она то появлялась,
			// то исчезала между Layout и Repaint, и IMGUI обрывал отрисовку окна
			if (SirenixEditorGUI.BeginFadeGroup(this, hasLabel && overflow && _expanded))
			{
				using (new GUILayout.HorizontalScope())
				{
					var textAreaStyle = new GUIStyle(GUI.skin.textArea);
					var padding = textAreaStyle.padding;
					padding.left += 3;
					padding.top += 3;
					padding.bottom = 4;
					textAreaStyle.padding = padding;

					GUILayout.TextArea(labelByKey ?? string.Empty, textAreaStyle);
					var textAreaRect = GUILayoutUtility.GetLastRect();

					EditorGUIUtility.AddCursorRect(textAreaRect, MouseCursor.Text);
				}
			}

			SirenixEditorGUI.EndFadeGroup();

			if (!_selector.show)
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretDownFill);
			else
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretUpFill);
		}

		private void TryResolveEntry()
		{
			if (ContentManager.initializing)
				return;

			ContentManager.TryGetEntry(Attribute.CatalogId, out _contentEntry);
		}

		private void TryCreateSelector()
		{
			if (_contentEntry == null)
				return;

			if (_selector != null && _cacheKeyCount == currentCatalog.Count)
				return;

			_selector = CreateSelector(in currentCatalog);
		}

		private GUIPopupSelector<int> CreateSelector(in KeyCatalog<int> catalog)
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
