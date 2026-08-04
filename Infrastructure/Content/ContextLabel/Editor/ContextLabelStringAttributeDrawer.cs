using Content.ScriptableObjects;
using Fusumity.Editor;
using Fusumity.Editor.Utility;
using Fusumity.Utility;
using Sapientia;
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
		private const float SUFFIX_MARGIN = 16f;

		private const float PEN_SIZE = 11f;
		private const float PEN_MARGIN = 19f;

		private int _cacheKeyCount = -1;
		private bool? _showedSelectorBeforeClick;
		private bool _expanded;

		private GUIPopupSelector<string> _selector;

		private UniqueContentEntry<ContextLabelCatalog<string>> _contentEntry;
		private ref readonly ContextLabelCatalog<string> currentCatalog => ref _contentEntry.Value;

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
				ValueEntry.SmartValue = SirenixEditorFields.TextField(label, selectedKey);
				return;
			}

			if (_selector == null)
				return;

			var isEmpty = selectedKey.IsNullOrEmpty();
			var contains = !isEmpty && currentCatalog.Contains(selectedKey);

			// [CanBeEmpty] на самом поле или на поле-контейнере — пустой ключ здесь не ошибка,
			// жёлтым подсвечиваем только реально невалидный (непустой, но не из каталога) ключ
			var acceptableEmpty = isEmpty && (Property.GetAttribute<CanBeEmptyAttribute>() != null ||
				Property.ParentValueProperty?.GetAttribute<CanBeEmptyAttribute>() != null);
			label ??= new GUIContent();
			EditorGUILayout.GetControlRect();

			var rect = GUILayoutUtility.GetLastRect();

			var selectorPopupRect = rect;
			var textFieldPosition = rect;
			var trianglePosition = rect.AlignRight(9f, 5f);

			// Ключа нет в каталоге — рядом со стрелкой ручка, чтобы завести его на месте,
			// не открывая ассет каталога
			var canAddKey = !isEmpty && !contains;
			var penPosition = canAddKey ? rect.AlignRight(PEN_SIZE, PEN_MARGIN) : default;

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

			if (canAddKey)
			{
				EditorGUIUtility.AddCursorRect(penPosition, MouseCursor.Arrow);
				if (GUI.Button(penPosition, new GUIContent(string.Empty, $"Add [ {selectedKey} ] to catalog"), GUIStyle.none))
					PromptAddKey(selectedKey);
			}

			var originalColor = GUI.color;

			var style = EditorStyles.textField;
			if (!contains && !acceptableEmpty)
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
			var hasLabel = !selectedKey.IsNullOrEmpty() && currentCatalog.TryGet(selectedKey, out labelByKey);

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

			selectedKey = ValueEntry.SmartValue = SirenixEditorFields.TextField(textFieldPosition, label, selectedKey, style);
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

			if (canAddKey)
				SdfIcons.DrawIcon(penPosition, SdfIconType.PencilFill);
		}

		private void PromptAddKey(string key)
		{
			EditorInputDialog.Show("Add Context Label",
				"Label",
				key.LastIndexOf('/') is var i && i >= 0 ? key[(i + 1)..].NicifyText() : key.NicifyText(),
				label => AddKey(key, label),
				$"Key: {key}");
		}

		private void AddKey(string key, string label)
		{
			if (Asset is not IContentEntryScriptableObject<ContextLabelCatalog<string>> entry)
			{
				Debug.LogError($"Catalog [ {Attribute.Catalog} ] is not editable");
				return;
			}

			Undo.RecordObject(Asset, "Add Context Label");
			entry.EditValue.SetEditor(key, label);
			EditorUtility.SetDirty(Asset);
			AssetDatabase.SaveAssetIfDirty(Asset);

			// Селектор пересоберётся сам: TryCreateSelector сверяет закешированное количество ключей
			GUIHelper.RequestRepaint();
		}

		// Насколько бы влез лейбл каталога рядом со значением поля (как в SuffixValue), без реальной отрисовки
		private static bool FitsSuffix(Rect fieldRect, GUIContent label, string value, string suffixText, GUIStyle valueStyle)
		{
			var labelWidth = label.text.IsNullOrEmpty() ? 0f : EditorGUIUtility.labelWidth;
			var available = fieldRect.width - labelWidth;
			var occupied = valueStyle.CalcWidth(value) + EditorStyles.miniLabel.CalcWidth(suffixText) + SUFFIX_MARGIN;
			return occupied <= available;
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

			var selector = new GUIPopupSelector<string>(keys.ToArray(),
				ValueEntry.SmartValue,
				HandleSelected,
				pathEvaluator: key =>
				{
					if (key == NONE_MENU)
						return NONE_MENU;

					// Ключ несёт иерархию через "/" (например "Common/RightHand/Sword"), а лейбл —
					// только имя листа: подставляем лейбл на место последнего сегмента ключа, чтобы
					// дерево селектора группировалось так же, как сгруппированы сами ключи каталога
					return BuildTreePath(key, currentCatalog[key]);
				});

			selector.SetSearchFunction(item =>
			{
				if (item.GetFullPath() == NONE_MENU)
					return false;

				if (item?.Value == null)
					return false;

				var key = (string) item.Value;
				var s = currentCatalog[key].ToLower();
				if (s.Contains(selector.GetSearchTerm().ToLower()))
					return true;
				return false;
			});

			// Серым рядом с именем — настоящий ключ каталога, чтобы по нему можно было узнать айди,
			// не разворачивая поле
			selector.SetSecondaryLabelEvaluator(key => key);

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

		// Ключ несёт иерархию через "/" (например "Common/RightHand/Sword") — сегменты-папки технические,
		// поэтому нисифицируем их (как ObjectNames.NicifyVariableName); лист заменяем на лейбл каталога,
		// он уже человекочитаемый и заведён вручную
		private static string BuildTreePath(string key, string leafLabel)
		{
			var separatorIndex = key.LastIndexOf('/');
			if (separatorIndex < 0)
				return leafLabel;

			using var _ = ListPool<string>.Get(out var segments);
			var start = 0;
			for (var i = 0; i <= separatorIndex; i++)
			{
				if (key[i] != '/')
					continue;

				segments.Add(key[start..i].NicifyText());
				start = i + 1;
			}

			segments.Add(leafLabel);
			return string.Join("/", segments);
		}

		private void SelectAsset()
		{
			EditorGUIUtility.PingObject(Asset);
		}

		private void PromptAddNew()
		{
			_selector.SetSelection(ValueEntry.SmartValue);
			if (Asset != null)
				GUIHelper.OpenInspectorWindow(Asset);
			_selector?.Hide();
		}

		private void HandleSelected(string key)
		{
			if (key == NONE_MENU)
				return;

			ValueEntry.WeakSmartValue = key;
		}

		private void TryResolveEntry()
		{
			if (ContentManager.initializing)
				return;

			ContentManager.TryGetEntry(Attribute.Catalog, out _contentEntry);
		}
	}
}
