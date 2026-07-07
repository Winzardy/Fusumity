using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using Fusumity.Editor;
using Fusumity.Editor.Utility;
using Fusumity.Utility;
using JetBrains.Annotations;
using Sapientia;
using Sapientia.Extensions;
using Sapientia.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Content.Editor
{
	using UnityObject = Object;

	public enum ContentDrawerMode
	{
		Undefined,
		String,
		Guid,
		Reference
	}

	public static class ContentReferenceConstants
	{
		public const string TOOLTIP_SPACE = "\n\n";
		public const string TOOLTIP_SINGLE_SPACE = "\n";
	}

	public class ContentReferenceArrayAttributeDrawer : OdinAttributeDrawer<ContentReferenceAttribute, ContentReference[]>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			CallNextDrawer(label);
		}
	}

	public class GuidContentReferenceAttributeDrawer : ContentReferenceAttributeDrawer<SerializableGuid>
	{
		protected override ContentDrawerMode TargetMode => ContentDrawerMode.Guid;
	}

	public class StringContentReferenceAttributeDrawer : ContentReferenceAttributeDrawer<string>
	{
		protected override ContentDrawerMode TargetMode => ContentDrawerMode.String;
	}

	public abstract class ContentReferenceAttributeDrawer<T> : OdinAttributeDrawer<ContentReferenceAttribute, T>, IDefinesGenericMenuItems
	{
		private const string NONE_LABEL = "None";
		private const string SCRIPTABLE_OBJECT_SUFFIX = "ScriptableObject";
		private const string CONFIG_SUFFIX = "Config";

		private bool _guidRawMode;
		private bool _creating;
		protected abstract ContentDrawerMode TargetMode { get; }

		private const string CONTROL_ID = "ContentReference";

		private static readonly string LABEL = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName == "ru"
			? "Идентификатор"
			: "Identifier";

		private static readonly string LABEL_GUID = "Guid";

		private static readonly string TOOLTIP_PREFIX = $"{LABEL}:\n".ColorText(Color.gray).SizeText(12);
		private static readonly string TOOLTIP_PREFIX_GUID = $"{LABEL_GUID}:\n".ColorText(Color.gray).SizeText(12);

		private bool _showDetailed;
		private ContentDrawerMode _mode = ContentDrawerMode.Undefined;
		private UnityObject _targetObject;
		private OdinEditor _inlineEditor;
		private GUIPopupSelector<ContentReferenceSelectorItem> _selector;
		private IContentEntrySource _selectorSource;
		private double _selectorClosedTime = -1;

		private IContentEntrySource _overlayIconSource;
		private Sprite _overlayIconSprite;

		private static readonly Color _iconOverlayBackground = EditorGUIUtility.isProSkin
			? new Color(40f / 255f, 40f / 255f, 40f / 255f)
			: new Color(209f / 255f, 209f / 255f, 209f / 255f);

		private readonly GUIContent _dropdownContent = new();

		// Защита от переоткрытия: popup Odin закрывается по потере фокуса раньше, чем инспектор обработает клик по кружку
		private const double SELECTOR_REOPEN_GUARD = 0.2;

		private const float SELECTOR_MIN_WIDTH = 220f;

		private static readonly GUIStyle _style = new(SirenixGUIStyles.CardStyle)
		{
			padding = new RectOffset(5, 3, 2, 3),
			margin = new RectOffset
			(
				SirenixGUIStyles.CardStyle.margin.left + 3,
				SirenixGUIStyles.CardStyle.margin.right + 3,
				SirenixGUIStyles.CardStyle.margin.top + 2,
				SirenixGUIStyles.CardStyle.margin.bottom
			)
		};

		private bool _nestedFoldout;

		private (string key, IContentEntrySource source, int contentVersion) _found;

		private Type _valueType;

		private static readonly Dictionary<Type, SelectorItemsCache> _selectorItemsByValueType = new();
		private static readonly Dictionary<Type, Type[]> _creatableConfigTypesByValueType = new();

		private int _selectorVersion = -1;
		private (int hash, PropertyTree tree) _targetToTree;

		public void PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu)
		{
			genericMenu.AddSeparator("");
			genericMenu.AddItem(new GUIContent("Set None"), false, HandleSetNoneClicked);
		}

		protected override void Initialize()
		{
			base.Initialize();

			if (Property.Parent?.ValueEntry?.WeakSmartValue is IContentReference _)
				_mode = ContentDrawerMode.Reference;
			else
				_mode = TargetMode;

			_valueType = Attribute.Type;
			if (_valueType == null)
			{
				var typeName = Attribute.TypeName;

				if (typeName.IsNullOrEmpty())
				{
					ContentDebug.LogError($"Target value type is null or type name is empty... (path: {Property.UnityPropertyPath})");
					return;
				}

				if (!ReflectionUtility.TryGetType(typeName, out _valueType))
				{
					ContentDebug.LogError($"Not found value type by name [ {Attribute.TypeName} ] (path: {Property.UnityPropertyPath})");
					return;
				}
			}
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (_valueType == null)
			{
				string errorMsg;
				if (!Attribute.TypeName.IsNullOrEmpty())
					errorMsg = $"Not found value type by name [ {Attribute.TypeName} ] (path: {Property.UnityPropertyPath})";
				else
					errorMsg = $"Target value type is null! (path: {Property.UnityPropertyPath})";

				ContentDebug.LogError(errorMsg);
				SirenixEditorGUI.ErrorMessageBox(errorMsg);
				return;
			}

			var targetLabel = new GUIContent(label ?? GUIContent.none);

			if (ContentManager.initializing)
			{
				EditorGUILayout.LabelField(targetLabel, new GUIContent("..."));
				return;
			}

			// Хак: убираем некорректный лейбл, проставленный редакторским кодом (через рефлексию)
			if (targetLabel.text.Contains("[") && targetLabel.text.Contains("]"))
				targetLabel.text = string.Empty;

			var isEmpty = true;
			var isSingle = false;
			IContentEntrySource source = null;

			switch (_mode)
			{
				case ContentDrawerMode.Guid:
					if (Property.ValueEntry.WeakSmartValue is SerializableGuid guid)
					{
						isEmpty = guid == SerializableGuid.Empty;
						source = !isEmpty ? FindSelectedSource(_valueType, in guid) : null;
					}

					break;
				case ContentDrawerMode.String:
					var id = (string) Property.ValueEntry.WeakSmartValue;
					isEmpty = id.IsNullOrEmpty();
					source = !isEmpty ? FindSelectedSource(_valueType, id) : null;
					break;
				case ContentDrawerMode.Reference:
					if (Property.Parent.ValueEntry.WeakSmartValue is IContentReference reference)
					{
						isSingle = reference.IsSingle;
						isEmpty = !isSingle && reference.Guid == SerializableGuid.Empty;
						source = !isEmpty ? FindSelectedSource(reference) : null;
					}

					break;
				default:
					return;
			}

			var invalid = source == null && !isEmpty;

			var originalIndent = EditorGUI.indentLevel;

			EditorGUI.BeginChangeCheck();

			GUI.SetNextControlName(CONTROL_ID);

			var originalTooltip = targetLabel.tooltip;

			var originEnabled = GUI.enabled;
			if (isSingle)
				GUI.enabled = false;

			if (!isSingle)
			{
				if (!invalid)
				{
					var guidStr = string.Empty;
					if (source is {ContentEntry: IUniqueContentEntry unique})
					{
						guidStr = unique.Guid.ToString();

						if (!targetLabel.tooltip.IsNullOrEmpty())
							targetLabel.tooltip += ContentReferenceConstants.TOOLTIP_SPACE;

						targetLabel.tooltip += $"{TOOLTIP_PREFIX_GUID}{guidStr}";
					}

					if (source is {ContentEntry: IIdentifiable identifiable})
					{
						var uniqueId = identifiable.Id;
						if (!uniqueId.IsNullOrEmpty() && !uniqueId.Contains(guidStr))
						{
							if (!targetLabel.tooltip.IsNullOrEmpty())
								targetLabel.tooltip += guidStr == string.Empty
									? ContentReferenceConstants.TOOLTIP_SPACE
									: ContentReferenceConstants.TOOLTIP_SINGLE_SPACE;

							targetLabel.tooltip += $"{TOOLTIP_PREFIX}{uniqueId}";
						}
					}
				}
			}
			else if (_mode == ContentDrawerMode.Reference)
			{
				var tryGetValue = ContentReferenceAttributeProcessor.propertyToGUIContent.TryGetValue(Property.Parent, out var GUIContent);
				if (tryGetValue)
				{
					targetLabel.text = GUIContent.text;
					targetLabel.tooltip = GUIContent.tooltip;
				}
				else
				{
					targetLabel.text = string.Empty;
				}
			}

			_targetObject = null;

			if (source is INestedContentEntrySource nested)
			{
				if (nested.Source is UnityObject obj)
					_targetObject = obj;
			}
			else if (source is UnityObject obj)
			{
				_targetObject = obj;
			}

			var useIndent = false;

			var drawerSettingsAttribute = Property.Attributes.GetAttribute<ContentReferenceDrawerSettingsAttribute>();
			drawerSettingsAttribute ??= Property.Parent.Attributes.GetAttribute<ContentReferenceDrawerSettingsAttribute>();

			var useInlineEditorBySettings = drawerSettingsAttribute?.InlineEditor ?? false;
			var useInlineEditor = useInlineEditorBySettings || Attribute.InlineEditor;

			//Костыль, потом подумаю как убрать, в Pack ломает отображение
			if (Property.ParentType == typeof(Pack<>)
				&& Property.Parent.Attributes.GetAttribute<HorizontalGroupAttribute>() != null)
				useInlineEditor = true;

			if (useInlineEditor && !EditorGUIUtility.hierarchyMode && _targetObject)
			{
				EditorGUI.indentLevel += 1;
				useIndent = true;
			}

			bool forceDisableInlineEditor = false;
			bool useDropdown;

			var originColor = GUI.color;
			{
				var errorColor = UnityObjectIsNullUtility.GetWarningColor(originColor);
				var canBeEmpty = Property.Info.GetAttribute<CanBeEmptyAttribute>() != null
					|| Property.Info.GetAttribute<CanBeNullAttribute>() != null
					|| Property.Info.GetAttribute<MaybeNullAttribute>() != null;
				var failIfEmpty = UnityObjectIsNullUtility.HasRequiredAttribute(Property);

				if (!canBeEmpty)
					if (typeof(IContentReference).IsAssignableFrom(Property.ParentType))
					{
						canBeEmpty = Property.ParentValueProperty.Info.GetAttribute<CanBeEmptyAttribute>() != null;
						failIfEmpty |= UnityObjectIsNullUtility.HasRequiredAttribute(Property.ParentValueProperty);
					}

				if (isEmpty
					&& GUI.enabled)
				{
					if (failIfEmpty)
						errorColor = UnityObjectIsNullUtility.GetInvalidColor(originColor);

					if (failIfEmpty || !canBeEmpty)
						GUI.color = errorColor;
				}

				var useDropdownBySettings = drawerSettingsAttribute?.Dropdown ?? false;
				useDropdown = Attribute.Dropdown || useDropdownBySettings;

				if (invalid)
				{
					errorColor = UnityObjectIsNullUtility.GetInvalidColor(originColor);
					GUI.color = errorColor;
				}

				source = DrawSourceSelector(targetLabel, source, useDropdown);

				if (!useDropdown && source is INestedContentEntrySource)
					forceDisableInlineEditor = true;
			}
			GUI.color = originColor;

			#region Inline Editor

			var labelWidthInEditor = GUIHelper.BetterLabelWidth - 4f;
			targetLabel.tooltip = originalTooltip;

			if (useInlineEditor && !forceDisableInlineEditor)
				TryCreateEditor();

			if (!forceDisableInlineEditor && _inlineEditor)
			{
				if (useInlineEditor)
				{
					GUIHelper.PushColor(Color.white);
					{
						var foldoutPosition = GUILayoutUtility.GetLastRect().AlignBottom(EditorGUIUtility.singleLineHeight);
						foldoutPosition.width = SirenixEditorGUI.FoldoutWidth;

						if (!EditorGUIUtility.hierarchyMode && _targetObject)
						{
							var offset = SirenixEditorGUI.FoldoutWidth + 3;
							foldoutPosition.x -= offset;
							foldoutPosition.width += offset;
						}

						var originEnabled2 = GUI.enabled;
						GUI.enabled = true;
						_showDetailed = SirenixEditorGUI.Foldout(foldoutPosition, _showDetailed, GUIContent.none);
						GUI.enabled = originEnabled2;

						if (SirenixEditorGUI.BeginFadeGroup(this, useInlineEditor && _showDetailed))
						{
							var originalColor = GUI.color;
							GUI.color = Color.black.WithAlpha(0.666f);

							//Hierarchy
							var originHierarchyMode = EditorGUIUtility.hierarchyMode;
							EditorGUIUtility.hierarchyMode = false;

							//Indent
							var originIndent = EditorGUI.indentLevel;
							if (useIndent)
								EditorGUI.indentLevel -= 1;

							SirenixEditorGUI.BeginIndentedVertical(_style);
							{
								GUIHelper.PushHierarchyMode(false);
								GUIHelper.PushLabelWidth(labelWidthInEditor);
								{
									GUI.color = originalColor;

									var originalForceHideMonoScriptInEditor = OdinEditor.ForceHideMonoScriptInEditor;
									OdinEditor.ForceHideMonoScriptInEditor = false;
									var originalDrawAssetReference = FusumityEditorGUIHelper.drawAssetReference;
									var originalDrawEnabledToggle = FusumityEditorGUIHelper.drawEnabledToggle;
									var originalDrawInlineEditor = FusumityEditorGUIHelper.drawInlineEditor;
									var originalAllowInlineEditorIdEditing = FusumityEditorGUIHelper.allowInlineEditorIdEditing;
									FusumityEditorGUIHelper.drawAssetReference = useDropdown;
									FusumityEditorGUIHelper.drawEnabledToggle = !useDropdown;
									FusumityEditorGUIHelper.drawInlineEditor = true;
									FusumityEditorGUIHelper.allowInlineEditorIdEditing = false;

									_inlineEditor.OnInspectorGUI();

									FusumityEditorGUIHelper.drawAssetReference = originalDrawAssetReference;
									FusumityEditorGUIHelper.drawEnabledToggle = originalDrawEnabledToggle;
									FusumityEditorGUIHelper.drawInlineEditor = originalDrawInlineEditor;
									FusumityEditorGUIHelper.allowInlineEditorIdEditing = originalAllowInlineEditorIdEditing;
									OdinEditor.ForceHideMonoScriptInEditor = originalForceHideMonoScriptInEditor;

									//Hierarchy/
									EditorGUIUtility.hierarchyMode = originHierarchyMode;
								}
								GUIHelper.PopLabelWidth();
								GUIHelper.PopHierarchyMode();

								//Indent/
								EditorGUI.indentLevel = originIndent;
							}

							SirenixEditorGUI.EndIndentedVertical();
						}

						SirenixEditorGUI.EndFadeGroup();
					}
					GUIHelper.PopColor();
				}
			}

			#endregion

			#region Nested

			if (source is INestedContentEntrySource nestedSource)
			{
				if (Property.Parent.ValueEntry.WeakSmartValue is not IContentReference reference)
					return;

				var rawValue = nestedSource.UniqueContentEntry?.RawValue;

				EditorGUI.indentLevel = originalIndent;
				var valid = rawValue != null && reference.ValueType.IsAssignableFrom(rawValue.GetType());
				FusumityEditorGUILayout.FoldoutContainer(Header, valid ? Body : null, ref _nestedFoldout, this);

				Rect Header()
				{
					var rect = EditorGUILayout.BeginHorizontal();

					var guid = reference.Guid;
					var output = FusumityEditorGUILayout.DrawGuidField(reference.Guid, targetLabel, ref _guidRawMode);
					if (GUI.enabled)
					{
						if (guid != output)
							Property.ValueEntry.WeakSmartValue = output;
					}

					if (!EditorGUIUtility.hierarchyMode)
						EditorGUI.indentLevel--;
					//TODO:добавить отображение GUID
					var halfWidth = FusumityEditorGUILayout.GetHalfFieldWidth();
					var sourceName = _targetObject ? _targetObject.name : NONE_LABEL;
					EditorGUILayout.LabelField(new GUIContent(sourceName, tooltip: "Source"), GUILayout.Width(halfWidth));

					if (!EditorGUIUtility.hierarchyMode)
						EditorGUI.indentLevel++;

					EditorGUILayout.EndHorizontal();

					if (!valid)
					{
						var msg = $" ContentEntry by guid [ {reference.Guid} ] is not of type [ {reference.ValueType} ]";
						SirenixEditorGUI.ErrorMessageBox(msg);
					}

					return rect;
				}

				void Body()
				{
					var originalColor = GUI.color;
					GUI.color = Color.black.WithAlpha(0.666f);

					var originHierarchyMode = EditorGUIUtility.hierarchyMode;
					EditorGUIUtility.hierarchyMode = false;

					if (useIndent)
						EditorGUI.indentLevel -= 1;

					SirenixEditorGUI.BeginIndentedVertical(_style);
					{
						GUIHelper.PushHierarchyMode(false);
						GUIHelper.PushLabelWidth(labelWidthInEditor);
						{
							GUI.color = originalColor;

							//Scripts
							var originalForceHideMonoScriptInEditor = OdinEditor.ForceHideMonoScriptInEditor;
							OdinEditor.ForceHideMonoScriptInEditor = false;
							var originalDrawAssetReference = FusumityEditorGUIHelper.drawAssetReference;
							FusumityEditorGUIHelper.drawAssetReference = false;
							{
								var hash = HashCode.Combine(Property, reference.Guid);
								if (_targetToTree.hash != hash)
								{
									_targetToTree.tree?.Dispose();
									_targetToTree.tree = PropertyTree.Create(rawValue, ValueEntry.SerializationBackend);
									_targetToTree.hash = hash;
								}

								EditorGUI.BeginChangeCheck();
								_targetToTree.tree.Draw(false);
								if (EditorGUI.EndChangeCheck())
								{
									_targetToTree.tree.ApplyChanges();
									Property.MarkSerializationRootDirty();
								}

								//Scripts/
							}
							FusumityEditorGUIHelper.drawAssetReference = originalDrawAssetReference;
							OdinEditor.ForceHideMonoScriptInEditor = originalForceHideMonoScriptInEditor;
						}
						GUIHelper.PopLabelWidth();
						GUIHelper.PopHierarchyMode();
					}
					EditorGUI.indentLevel = originalIndent;

					SirenixEditorGUI.EndIndentedVertical();
					EditorGUIUtility.hierarchyMode = originHierarchyMode;
				}
			}

			#endregion

			if (EditorGUI.EndChangeCheck())
			{
				if (source is IUniqueContentEntrySource uniqueSource && uniqueSource.Id.IsNullOrEmpty())
					ContentDebug.LogError("Failed to assign source - new source is empty.");
				else
					UpdateValue();
			}

			void UpdateValue()
			{
				ApplySource(source);
			}

			EditorGUI.indentLevel = originalIndent;
			GUI.enabled = originEnabled;
		}

		private void HandleSetNoneClicked()
		{
			SetNoneInternal();
		}

		private void SetNoneInternal()
		{
			Property.ValueEntry.WeakSmartValue = _mode switch
			{
				ContentDrawerMode.String => null,
				ContentDrawerMode.Guid or ContentDrawerMode.Reference => SerializableGuid.Empty,
				_ => null
			};
		}

		private IContentEntrySource DrawSourceSelector(GUIContent label, IContentEntrySource source, bool asDropdown)
		{
			TryCreateSelector(source);

			var rect = EditorGUILayout.GetControlRect();

			if (asDropdown)
			{
				var fieldRect = label.text.IsNullOrEmpty() ? rect : EditorGUI.PrefixLabel(rect, label);

				var id = source is {ContentEntry: IIdentifiable identifiable} ? identifiable.Id : null;
				_dropdownContent.text = id.IsNullOrEmpty() ? NONE_LABEL : id;

				if (_selector != null && EditorGUI.DropdownButton(fieldRect, _dropdownContent, FocusType.Keyboard))
					OpenSelector(fieldRect);
			}
			else
			{
				var popupRect = rect;
				if (!label.text.IsNullOrEmpty())
				{
					popupRect.width -= EditorGUIUtility.labelWidth;
					popupRect.x += EditorGUIUtility.labelWidth;
				}

				var pickerRect = rect.AlignRight(18f);
				EditorGUIUtility.AddCursorRect(pickerRect, MouseCursor.Arrow);

				// Перехватываем клик по кружку ДО ObjectField и гасим событие — иначе откроется нативный пикер
				var e = Event.current;
				if (e.type == EventType.MouseDown && e.button == 0 && pickerRect.Contains(e.mousePosition))
				{
					OpenSelector(popupRect);
					e.Use();
				}

				EditorGUI.ObjectField(rect, label, _targetObject, GetObjectFieldType(source), false);

				DrawSourceIconOverlay(rect, label, source);
			}

			return source;
		}

		private void DrawSourceIconOverlay(Rect rect, GUIContent label, IContentEntrySource source)
		{
			if (Event.current.type != EventType.Repaint || !_targetObject)
				return;

			if (!ReferenceEquals(_overlayIconSource, source))
			{
				_overlayIconSource = source;
				_overlayIconSprite = ContentPreviewUtility.GetPreviewIcon(source);
			}

			var sprite = _overlayIconSprite;
			if (!sprite || !sprite.texture)
				return;

			// Повторяем математику EditorGUI.ObjectField -> PrefixLabel (без аллокации control id):
			// пустой лейбл -> IndentedRect (учёт indent), иначе -> x + labelWidth + 2
			Rect fieldRect;
			if (label != null && (!label.text.IsNullOrEmpty() || label.image != null))
			{
				fieldRect = rect;
				fieldRect.xMin += EditorGUIUtility.labelWidth + 2f;
			}
			else
			{
				fieldRect = EditorGUI.IndentedRect(rect);
			}

			// Иконка объекта — 12px у левого края поля, по центру по вертикали. Рисуем чуть крупнее для перекрытия
			const float iconSize = 13f;
			var iconRect = new Rect(fieldRect.x + 2f, fieldRect.y + (fieldRect.height - iconSize) * 0.5f, iconSize, iconSize);

			// Фон, чтобы скрыть дефолтную иконку (у спрайтов прозрачный фон)
			EditorGUI.DrawRect(iconRect, _iconOverlayBackground);

			FusumityEditorGUILayout.DrawObjectFieldIconSprite(iconRect, sprite);
		}

		// Открывает кастомный селектор, не переоткрывая его тем же кликом, что его закрыл (потеря фокуса popup'а)
		private void OpenSelector(Rect rect)
		{
			if (_selector == null || _selector.show)
				return;

			if (EditorApplication.timeSinceStartup - _selectorClosedTime < SELECTOR_REOPEN_GUARD)
				return;

			// Ширина фиксированная, высота 0 — включает встроенную авто-подгонку Odin (EnableAutomaticHeightAdjustment):
			// она измеряет реально отрисованный контент и сама вписывает окно в рабочую область экрана (с переворотом
			// вверх при нехватке места снизу). Ручной расчёт по константам был неточным и оставлял пустой хвост
			var width = Mathf.Max(rect.width, SELECTOR_MIN_WIDTH);

			if (_selector.ShowPopup(rect, new Vector2(width, 0f)) && _selector.Window != null)
				_selector.Window.OnClose += MarkSelectorClosed;
		}

		private void MarkSelectorClosed()
		{
			_selectorClosedTime = EditorApplication.timeSinceStartup;
		}

		private void TryCreateSelector(IContentEntrySource source)
		{
			var version = ContentEditorCache.version;
			var items = GetSelectorItems(_valueType);
			if (_selector == null || _selectorVersion != version)
			{
				_selectorVersion = version;
				_selectorSource = source;
				_selector = new GUIPopupSelector<ContentReferenceSelectorItem>(
					items,
					FindSelectorItem(items, source),
					HandleSelectorItemSelected,
					pathEvaluator: static item => item?.Path ?? NONE_LABEL);

				_selector.SetIconEvaluator(static item => ContentPreviewUtility.GetPreviewIcon(item?.Source));
				_selector.SetSecondaryLabelEvaluator(GetSelectorSecondaryLabel);

				// Кнопка "+" в тулбаре popup'а — создать новый config
				_selector.AddToolbarFunctionButtons(new FunctionButtonInfo
				{
					sdfIcon = SdfIconType.Plus,
					action = () =>
					{
						// Всё — вне текущего IMGUI-прохода попапа: Hide() закрывает окно попапа, и если вызвать его
						// синхронно из отрисовки тулбара, оставшийся DrawEditorPreview падает на обнулённых editors
						var source = _selectorSource;
						EditorApplication.delayCall += () =>
						{
							_selector?.Hide();
							PromptCreateSource(source);
						};
					}
				});

				_selector.SetSearchFunction(item =>
				{
					if (item?.Value is not ContentReferenceSelectorItem selectorItem ||
						selectorItem.Kind != SelectorItemKind.Source)
					{
						return false;
					}

					var searchTerm = _selector.GetSearchTerm();
					return !searchTerm.IsNullOrEmpty() &&
						selectorItem.Path.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
				});

				return;
			}

			// Обновляем выделение только при смене источника — иначе линейный поиск + SetSelection каждый кадр
			if (!IsSameSource(_selectorSource, source))
			{
				_selectorSource = source;
				_selector.SetSelection(FindSelectorItem(items, source));
			}
		}

		private void HandleSelectorItemSelected(ContentReferenceSelectorItem selected)
		{
			if (selected == null)
				return;

			if (selected.Kind == SelectorItemKind.None)
				ApplySource(null);
			else
				ApplySource(selected.Source);
		}

		private static Type GetObjectFieldType(IContentEntrySource source)
		{
			return TryGetSourceObject(source, out var obj) && obj ? obj.GetType() : typeof(UnityObject);
		}

		private void ApplySource(IContentEntrySource source)
		{
			if (source is IUniqueContentEntrySource unique)
			{
				Property.ValueEntry.WeakSmartValue = _mode switch
				{
					ContentDrawerMode.String => unique.Id,
					ContentDrawerMode.Guid or ContentDrawerMode.Reference => unique.UniqueContentEntry.Guid,
					_ => Property.ValueEntry.WeakSmartValue
				};

				_found = (unique.UniqueContentEntry.Guid.ToString(), source, ContentEditorCache.version);
			}
			else
			{
				SetNoneInternal();
				_found = default;
			}

			Property.MarkSerializationRootDirty();
			GUIHelper.RequestRepaint();
		}

		private void PromptCreateSource(IContentEntrySource currentSource)
		{
			if (_creating)
				return;

			var configTypes = GetCreatableConfigTypes(_valueType);
			if (configTypes.IsNullOrEmpty())
			{
				ContentDebug.LogError($"Not found creatable config type by value type [ {_valueType.Name} ]");
				return;
			}

			if (configTypes.Length == 1)
			{
				PromptCreateSource(configTypes[0], currentSource);
				return;
			}

			var menu = new GenericMenu();
			for (int i = 0; i < configTypes.Length; i++)
			{
				var configType = configTypes[i];
				menu.AddItem(new GUIContent($"Add New {GetConfigDisplayName(configType)}"), false,
					() => PromptCreateSource(configType, currentSource));
			}

			menu.ShowAsContext();
		}

		private void PromptCreateSource(Type configType, IContentEntrySource currentSource)
		{
			if (_creating || !CanCreateContentEntry(configType))
				return;

			var folder = GetCreateFolder(currentSource);
			if (folder.IsNullOrEmpty())
				return;

			var defaultAssetName = GetDefaultAssetName(configType);
			ContentReferenceCreateConfigNameWindow.Open($"New {GetConfigDisplayName(configType)}", defaultAssetName, folder,
				SanitizeAssetName, NormalizeCreateFolderPath,
				(assetName, assetFolder) => CreateContentEntry(configType, assetFolder, assetName));
		}

		private void CreateContentEntry(Type configType, string folder, string assetName)
		{
			if (_creating || !CanCreateContentEntry(configType) || folder.IsNullOrEmpty())
				return;

			assetName = SanitizeAssetName(assetName);
			if (assetName.IsNullOrEmpty())
				return;

			_creating = true;
			try
			{
				AssetDatabaseUtility.EnsureOrCreateFolder(folder);

				var assetPath = GetUniqueAssetPath(folder, assetName);
				var asset = ScriptableObject.CreateInstance(configType);
				if (asset == null)
					return;

				AssetDatabase.CreateAsset(asset, assetPath);
				AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
				AssetDatabase.SaveAssets();

				var created = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
				if (created == null)
					return;

				ContentEditorCache.ClearAndRefreshScrObjs();
				_selectorItemsByValueType.Remove(_valueType);

				if (TryGetCreatedSource(created, out var createdSource))
					ApplySource(createdSource);
				else
					ScheduleApplyCreatedSource(created);

				Selection.activeObject = created;
				EditorGUIUtility.PingObject(created);
			}
			finally
			{
				_creating = false;
			}
		}

		private bool TryGetCreatedSource(ScriptableObject created, out IContentEntrySource source)
		{
			source = null;
			if (created is not IUniqueContentEntrySource unique)
				return false;

			var guid = unique.Guid;
			if (guid != SerializableGuid.Empty &&
				ContentEditorCache.TryGetSource(_valueType, in guid, out source))
			{
				return true;
			}

			var id = unique.Id;
			return !id.IsNullOrEmpty() &&
				ContentEditorCache.TryGetSource(_valueType, id, out source);
		}

		private void ScheduleApplyCreatedSource(ScriptableObject created)
		{
			EditorApplication.delayCall += () =>
			{
				if (!created)
					return;

				ContentEditorCache.ClearAndRefreshScrObjs();
				_selectorItemsByValueType.Remove(_valueType);

				if (TryGetCreatedSource(created, out var createdSource))
					ApplySource(createdSource);
			};
		}

		private static ContentReferenceSelectorItem[] GetSelectorItems(Type valueType)
		{
			var version = ContentEditorCache.version;
			if (_selectorItemsByValueType.TryGetValue(valueType, out var cache) &&
				cache.version == version)
			{
				return cache.items;
			}

			var sources = ContentEditorCache.GetAllSourceByValueType(valueType);
			var items = new List<ContentReferenceSelectorItem>
			{
				ContentReferenceSelectorItem.None
			};

			foreach (var source in sources)
			{
				if (source is not IUniqueContentEntrySource)
					continue;

				items.Add(ContentReferenceSelectorItem.Create(source, GetSelectorPath(source)));
			}

			items.Sort(CompareSelectorItems);

			var itemArray = items.ToArray();
			_selectorItemsByValueType[valueType] = new SelectorItemsCache
			{
				version = version,
				items = itemArray
			};

			return itemArray;
		}

		private static int CompareSelectorItems(ContentReferenceSelectorItem left, ContentReferenceSelectorItem right)
		{
			if (left.Kind != right.Kind)
				return left.Kind.CompareTo(right.Kind);

			return string.Compare(left.Path, right.Path, StringComparison.OrdinalIgnoreCase);
		}

		private static ContentReferenceSelectorItem FindSelectorItem(
			ContentReferenceSelectorItem[] items,
			IContentEntrySource source)
		{
			if (source == null)
				return ContentReferenceSelectorItem.None;

			for (int i = 0; i < items.Length; i++)
			{
				var item = items[i];
				if (item.Kind == SelectorItemKind.Source && IsSameSource(item.Source, source))
					return item;
			}

			return ContentReferenceSelectorItem.None;
		}

		private static bool IsSameSource(IContentEntrySource left, IContentEntrySource right)
		{
			if (ReferenceEquals(left, right))
				return true;

			if (left is IUniqueContentEntrySource leftUnique &&
				right is IUniqueContentEntrySource rightUnique)
			{
				return leftUnique.UniqueContentEntry.Guid == rightUnique.UniqueContentEntry.Guid;
			}

			return false;
		}

		private static string GetSourceName(IContentEntrySource source)
		{
			if (TryGetSourceObject(source, out var obj) && obj)
				return obj.name;

			if (source is {ContentEntry: IIdentifiable identifiable} &&
				!identifiable.Id.IsNullOrEmpty())
			{
				return identifiable.Id;
			}

			return NONE_LABEL;
		}

		// Полное имя ассета серым рядом с коротким именем — только для вложенных в категорию (в Id есть '/')
		private static string GetSelectorSecondaryLabel(ContentReferenceSelectorItem item)
		{
			if (item == null || item.Kind != SelectorItemKind.Source ||
				item.Path.IsNullOrEmpty() || item.Path.IndexOf('/') < 0)
				return null;

			return TryGetSourceObject(item.Source, out var obj) && obj ? obj.name : null;
		}

		// Путь пунктов строится по Id: "Relic/Test/New" даёт вложенность, Id без '/' — плоский пункт
		private static string GetSelectorPath(IContentEntrySource source)
		{
			if (source is {ContentEntry: IIdentifiable identifiable} && !identifiable.Id.IsNullOrEmpty())
				return identifiable.Id;

			return GetSourceName(source);
		}

		private static bool TryGetSourceObject(IContentEntrySource source, out UnityObject obj)
		{
			if (source is INestedContentEntrySource nested && nested.Source is UnityObject nestedObj)
			{
				obj = nestedObj;
				return true;
			}

			obj = source as UnityObject;
			return obj;
		}

		private static Type[] GetCreatableConfigTypes(Type valueType)
		{
			if (_creatableConfigTypesByValueType.TryGetValue(valueType, out var cachedTypes))
				return cachedTypes;

			var types = new List<Type>();
			foreach (var type in TypeCache.GetTypesDerivedFrom<IUniqueContentEntrySource>())
			{
				if (!CanCreateContentEntry(type))
					continue;

				if (TryGetContentEntryValueType(type, out var entryValueType) && entryValueType == valueType)
					types.Add(type);
			}

			types.Sort(static (x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
			cachedTypes = types.ToArray();
			_creatableConfigTypesByValueType[valueType] = cachedTypes;
			return cachedTypes;
		}

		private static bool TryGetContentEntryValueType(Type type, out Type valueType)
		{
			foreach (var interfaceType in type.GetInterfaces())
			{
				if (interfaceType.IsGenericType &&
					interfaceType.GetGenericTypeDefinition() == typeof(IUniqueContentEntrySource<>))
				{
					valueType = interfaceType.GetGenericArguments()[0];
					return true;
				}
			}

			valueType = null;
			return false;
		}

		private static bool CanCreateContentEntry(Type type)
		{
			return type != null &&
				typeof(ScriptableObject).IsAssignableFrom(type) &&
				typeof(IUniqueContentEntrySource).IsAssignableFrom(type) &&
				!type.IsAbstract &&
				!type.IsGenericTypeDefinition;
		}

		private string GetCreateFolder(IContentEntrySource currentSource)
		{
			var folder = GetAssetFolder(currentSource);
			if (!folder.IsNullOrEmpty())
				return folder;

			var items = GetSelectorItems(_valueType);
			for (int i = 0; i < items.Length; i++)
			{
				folder = GetAssetFolder(items[i].Source);
				if (!folder.IsNullOrEmpty())
					return folder;
			}

			return "Assets/Database";
		}

		private static string GetAssetFolder(IContentEntrySource source)
		{
			if (source == null || !TryGetSourceObject(source, out var obj))
				return null;

			return GetAssetFolder(obj);
		}

		private static string GetAssetFolder(UnityObject asset)
		{
			if (asset == null)
				return null;

			var path = AssetDatabase.GetAssetPath(asset);
			if (path.IsNullOrEmpty())
				return null;

			if (AssetDatabase.IsValidFolder(path))
				return NormalizeAssetPath(path);

			return NormalizeAssetPath(Path.GetDirectoryName(path));
		}

		private static string GetDefaultAssetName(Type type)
		{
			var assetName = GetConfigTypeName(type);
			if (assetName.IsNullOrEmpty() && type != null)
				assetName = type.Name;
			if (assetName.IsNullOrEmpty())
				assetName = "Asset";

			return $"{assetName}_New";
		}

		private static string GetConfigTypeName(Type type)
		{
			if (type == null)
				return null;

			var raw = type.Name;
			var stripped = TrimTypeSuffix(raw, SCRIPTABLE_OBJECT_SUFFIX);
			stripped = TrimTypeSuffix(stripped, CONFIG_SUFFIX);

			return stripped.IsNullOrEmpty() ? raw : stripped;
		}

		private static string GetConfigDisplayName(Type type)
		{
			var name = GetConfigTypeName(type);
			return name.IsNullOrEmpty() ? "Asset" : ObjectNames.NicifyVariableName(name);
		}

		private static string TrimTypeSuffix(string value, string suffix)
		{
			return !value.IsNullOrEmpty() && value.EndsWith(suffix, StringComparison.Ordinal)
				? value[..^suffix.Length]
				: value;
		}

		private static string SanitizeAssetName(string assetName)
		{
			if (assetName.IsNullOrEmpty())
				return null;

			assetName = assetName.Trim();
			if (assetName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
				assetName = assetName[..^".asset".Length];

			if (assetName.IsNullOrEmpty())
				return null;

			var invalidChars = Path.GetInvalidFileNameChars();
			var builder = new StringBuilder(assetName.Length);
			for (int i = 0; i < assetName.Length; i++)
			{
				var character = assetName[i];
				builder.Append(IsInvalidAssetNameCharacter(character, invalidChars) ? '_' : character);
			}

			var sanitized = builder.ToString().Trim();
			return sanitized.IsNullOrEmpty() ? null : sanitized;
		}

		private static bool IsInvalidAssetNameCharacter(char character, char[] invalidChars)
		{
			return Array.IndexOf(invalidChars, character) >= 0 ||
				character is '/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|';
		}

		private static string NormalizeCreateFolderPath(string folder)
		{
			if (folder.IsNullOrEmpty())
				return null;

			folder = NormalizeAssetPath(folder.Trim())?.TrimEnd('/');
			if (folder == "Assets" || folder.StartsWith("Assets/", StringComparison.Ordinal))
				return folder;

			var dataPath = NormalizeAssetPath(Application.dataPath);
			if (string.Equals(folder, dataPath, StringComparison.Ordinal))
				return "Assets";

			return folder.StartsWith(dataPath + "/", StringComparison.Ordinal)
				? "Assets" + folder[dataPath.Length..]
				: null;
		}

		private static string GetUniqueAssetPath(string folder, string assetName)
		{
			var assetPath = $"{folder}/{assetName}.asset";
			if (AssetDatabase.LoadAssetAtPath<UnityObject>(assetPath) == null)
				return assetPath;

			for (int index = 2;; index++)
			{
				assetPath = $"{folder}/{assetName}_{index}.asset";
				if (AssetDatabase.LoadAssetAtPath<UnityObject>(assetPath) == null)
					return assetPath;
			}
		}

		private static string NormalizeAssetPath(string path)
		{
			return path?.Replace('\\', '/');
		}

		private sealed class SelectorItemsCache
		{
			public int version;
			public ContentReferenceSelectorItem[] items;
		}

		private enum SelectorItemKind
		{
			None,
			Source
		}

		private sealed class ContentReferenceSelectorItem
		{
			public static readonly ContentReferenceSelectorItem None = new(SelectorItemKind.None, null, NONE_LABEL);

			public SelectorItemKind Kind { get; }
			public IContentEntrySource Source { get; }
			public string Path { get; }

			private ContentReferenceSelectorItem(SelectorItemKind kind, IContentEntrySource source, string path)
			{
				Kind = kind;
				Source = source;
				Path = path;
			}

			public static ContentReferenceSelectorItem Create(IContentEntrySource source, string path) => new(SelectorItemKind.Source, source, path);

			public override string ToString()
			{
				return Path;
			}
		}

		private void TryCreateEditor()
		{
			if (_targetObject != null)
			{
				if (_inlineEditor == null)
				{
					_inlineEditor = (OdinEditor) OdinEditor.CreateEditor(_targetObject);
				}
				else if (_inlineEditor.target != _targetObject)
				{
					OdinEditor.DestroyImmediate(_inlineEditor);
					_inlineEditor = null;

					_inlineEditor = (OdinEditor) OdinEditor.CreateEditor(_targetObject);
				}
			}
			else if (_inlineEditor != null)
			{
				OdinEditor.DestroyImmediate(_inlineEditor);
				_inlineEditor = null;
			}
		}

		private IContentEntrySource FindSelectedSource(Type type, string id)
		{
			if (_found.key == id)
				if (_found.contentVersion == ContentEditorCache.version)
					return _found.source;

			if (ContentEditorCache.TryGetSource(type, id, out var source))
			{
				_found = (id, source, ContentEditorCache.version);
				return source;
			}

			_found = (id, null, ContentEditorCache.version);
			return null;
		}

		private IContentEntrySource FindSelectedSource(Type type, in SerializableGuid guid)
		{
			if (_found.key == guid.ToString())
				if (_found.contentVersion == ContentEditorCache.version)
					return _found.source;

			if (ContentEditorCache.TryGetSource(type, in guid, out var source))
			{
				_found = (guid.ToString(), source, ContentEditorCache.version);
				return source;
			}

			_found = (guid.ToString(), null, ContentEditorCache.version);
			return null;
		}

		private IContentEntrySource FindSelectedSource(IContentReference reference)
		{
			if (_found.key == reference.Guid.ToString())
				if (_found.contentVersion == ContentEditorCache.version)
					return _found.source;

			if (ContentEditorCache.TryGetSource(reference, _valueType, out var source))
			{
				_found = (reference.Guid.ToString(), source, ContentEditorCache.version);
				return source;
			}

			_found = (reference.Guid.ToString(), null, ContentEditorCache.version);
			return null;
		}
	}

	internal class ContentReferenceCreateConfigNameWindow : OdinEditorWindow
	{
		private const float WIDTH = 430f;
		private const float HEIGHT = 120f;

		[ShowInInspector]
		[InlineProperty]
		[HideLabel]
		[PropertyOrder(0)]
		private AssetFullPath _assetPath;

		private Func<string, string> _sanitizeAssetName;
		private Func<string, string> _normalizeFolderPath;
		private Action<string, string> _onSubmit;

		public static void Open(
			string title,
			string defaultAssetName,
			string defaultFolder,
			Func<string, string> sanitizeAssetName,
			Func<string, string> normalizeFolderPath,
			Action<string, string> onSubmit)
		{
			var window = CreateInstance<ContentReferenceCreateConfigNameWindow>();
			window.titleContent = new GUIContent(title);
			window._assetPath = new AssetFullPath
			{
				path = defaultFolder,
				name = defaultAssetName
			};
			window._sanitizeAssetName = sanitizeAssetName;
			window._normalizeFolderPath = normalizeFolderPath;
			window._onSubmit = onSubmit;
			window.minSize = new Vector2(WIDTH, HEIGHT);
			window.maxSize = new Vector2(WIDTH, HEIGHT);
			window.position = GUIHelper.GetEditorWindowRect().AlignCenter(WIDTH, HEIGHT);
			window.ShowUtility();
			window.Focus();
		}

		[ButtonGroup("Actions")]
		[Button("Cancel")]
		[PropertyOrder(10)]
		private void Cancel()
		{
			Close();
		}

		[ButtonGroup("Actions")]
		[Button("Create")]
		[EnableIf(nameof(CanSubmit))]
		[PropertyOrder(10)]
		private void Submit()
		{
			var assetName = _sanitizeAssetName?.Invoke(_assetPath.name);
			var folder = _normalizeFolderPath?.Invoke(_assetPath.path);
			if (assetName.IsNullOrEmpty() || folder.IsNullOrEmpty())
				return;

			var onSubmit = _onSubmit;
			Close();
			EditorApplication.delayCall += () => onSubmit?.Invoke(assetName, folder);
		}

		private bool CanSubmit()
		{
			var assetName = _sanitizeAssetName?.Invoke(_assetPath.name);
			var folder = _normalizeFolderPath?.Invoke(_assetPath.path);
			return !assetName.IsNullOrEmpty() &&
				!folder.IsNullOrEmpty() &&
				AssetDatabase.IsValidFolder(folder);
		}

		[InlineProperty]
		[Serializable]
		private struct AssetFullPath
		{
			private const string EXTENSION = ".asset";

			[HorizontalGroup]
			[HideLabel, FolderPath]
			public string path;

			[HorizontalGroup(width: 0.35f)]
			[HideLabel, SuffixLabel(EXTENSION)]
			public string name;

			public override string ToString() => Path.Combine(path, name + EXTENSION);
		}
	}
}
