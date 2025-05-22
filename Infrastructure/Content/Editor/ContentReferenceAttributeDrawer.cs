using System;
using System.Collections.Generic;
using System.Globalization;
using Fusumity.Editor;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Collections;
using Sapientia.Extensions;
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

	public class ContentReferenceDrawer : OdinValueDrawer<ContentReference>
	{
		public const string TOOLTIP_SPACE = "\n\n";

		private bool _guidRawMode;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var output = FusumityEditorGUILayout.DrawGuidField(ValueEntry.SmartValue, label, ref _guidRawMode);

			if (!GUI.enabled)
				return;

			ValueEntry.SmartValue = output;
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

	public abstract class ContentReferenceAttributeDrawer<T> : OdinAttributeDrawer<ContentReferenceAttribute, T>
	{
		private bool _guidRawMode;
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

		private readonly Dictionary<Type, bool> _keyToSearchResult = new();

		private static Dictionary<Type, Type> _valueTypeToSourceType = new();

		private (int hash, PropertyTree tree) _targetToTree;

		protected override void Initialize()
		{
			base.Initialize();

			if (Property.Parent.ValueEntry.WeakSmartValue is IContentReference _)
				_mode = ContentDrawerMode.Reference;
			else
				_mode = TargetMode;

			if (!_valueTypeToSourceType.ContainsKey(Attribute.Type))
			{
				var targetType = typeof(IUniqueContentEntrySource<>).MakeGenericType(Attribute.Type);
				var types = targetType.GetAllTypes();

				if (types.Count == 1)
					targetType = types.First();

				_valueTypeToSourceType[Attribute.Type] = targetType;
			}
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var targetLabel = new GUIContent(label ?? GUIContent.none);
			if (targetLabel.text.Contains("[") && targetLabel.text.Contains("]"))
				targetLabel.text = string.Empty;

			string invalidLabel = null;
			var isEmpty = true;
			var isSingle = false;
			IContentEntrySource source = null;

			var valueType = Attribute.Type;

			switch (_mode)
			{
				case ContentDrawerMode.Guid:
					if (Property.ValueEntry.WeakSmartValue is SerializableGuid guid)
					{
						isEmpty = guid == SerializableGuid.Empty;
						source = !isEmpty ? FindSelectedSource(valueType, in guid) : null;
						invalidLabel = guid.ToString();
					}

					break;
				case ContentDrawerMode.String:
					var id = (string) Property.ValueEntry.WeakSmartValue;
					isEmpty = id.IsNullOrEmpty();
					source = !isEmpty ? FindSelectedSource(valueType, id) : null;
					invalidLabel = id;
					break;
				case ContentDrawerMode.Reference:
					if (Property.Parent.ValueEntry.WeakSmartValue is IContentReference reference)
					{
						isSingle = reference.IsSingle;
						isEmpty = !isSingle && reference.Guid == SerializableGuid.Empty;
						source = !isEmpty ? FindSelectedSource(reference) : null;
						invalidLabel = isSingle
							? $"{reference.ValueType.Name} (not found single entry by type)"
							: reference.Guid.ToString();
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
							targetLabel.tooltip += ContentReferenceDrawer.TOOLTIP_SPACE;

						targetLabel.tooltip += $"{TOOLTIP_PREFIX_GUID}{guidStr}";
					}

					if (source is {ContentEntry: IIdentifiable identifiable})
					{
						var uniqueId = identifiable.Id;
						if (!uniqueId.IsNullOrEmpty() && !uniqueId.Contains(guidStr))
						{
							if (!targetLabel.tooltip.IsNullOrEmpty())
								targetLabel.tooltip += ContentReferenceDrawer.TOOLTIP_SPACE;

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
			var forceHideFoldout = Property.Attributes.GetAttribute<HideFoldoutAttribute>() != null ||
				Property.Parent.Attributes.GetAttribute<HideFoldoutAttribute>() != null;

			//Костыль, потом подумаю как убрать, в Pack ломает отображение
			if (Property.Parent.Attributes.GetAttribute<HorizontalGroupAttribute>() != null)
				forceHideFoldout = true;

			var useFoldout = !forceHideFoldout && Attribute.Foldout;
			if (useFoldout && !EditorGUIUtility.hierarchyMode && _targetObject)
			{
				EditorGUI.indentLevel += 1;
				useIndent = true;
			}

			var originColor = GUI.color;
			Rect? objectFieldPosition = null;
			if (invalid)
			{
				EditorGUILayout.LabelField(targetLabel);
				var position = GUILayoutUtility.GetLastRect().AlignBottom(EditorGUIUtility.singleLineHeight);
				if (!targetLabel.text.IsNullOrEmpty())
				{
					position.width -= EditorGUIUtility.labelWidth;
					position.x += EditorGUIUtility.labelWidth;
				}

				targetLabel = GUIContent.none;

				GUI.color = Color.red;

				var cacheGuiEnabled = GUI.enabled;
				GUI.enabled = false;

				GUI.enabled = false;

				SirenixEditorFields.TextField(position, targetLabel, "");

				GUI.enabled = cacheGuiEnabled;

				var labelPos = position;
				labelPos.x += 3;
				EditorGUI.LabelField(labelPos, invalidLabel);

				position.x += position.width - 20;
				position.width = 20;
				objectFieldPosition = position;
			}

			var forceDisableInlineEditor = false;
			if (isSingle || invalid)
			{
				if (objectFieldPosition.HasValue)
				{
					source = (IContentEntrySource) EditorGUI.ObjectField
					(
						objectFieldPosition.Value,
						targetLabel,
						_targetObject,
						_valueTypeToSourceType[valueType],
						false
					);
				}
				else
				{
					source = (IContentEntrySource) EditorGUILayout.ObjectField
					(
						targetLabel,
						_targetObject,
						_valueTypeToSourceType[valueType],
						false
					);
				}
			}
			else
			{
				if (source is INestedContentEntrySource _)
				{
					forceDisableInlineEditor = true;
				}
				else
				{
					source = (IContentEntrySource) SirenixEditorFields.UnityObjectField
					(
						targetLabel,
						_targetObject,
						_valueTypeToSourceType[valueType],
						false
					);
				}
			}

			GUI.color = originColor;

			#region Inline Editor

			var labelWidthInEditor = GUIHelper.BetterLabelWidth - 4f;
			targetLabel.tooltip = originalTooltip;

			if (!forceDisableInlineEditor)
				TryCreateEditor();

			if (!forceDisableInlineEditor && _inlineEditor)
			{
				if (useFoldout)
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

					if (SirenixEditorGUI.BeginFadeGroup(this, useFoldout && _showDetailed))
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

								//Scripts
								var originalForceHideMonoScriptInEditor = OdinEditor.ForceHideMonoScriptInEditor;
								OdinEditor.ForceHideMonoScriptInEditor = false;
								var originalDrawAssetReference = FusumityEditorGUIHelper.drawAssetReference;
								FusumityEditorGUIHelper.drawAssetReference = false;

								_inlineEditor.OnInspectorGUI();

								//Scripts/
								FusumityEditorGUIHelper.drawAssetReference = originalDrawAssetReference;
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
			}

			#endregion

			#region Nested

			if (source is INestedContentEntrySource nestedSource)
			{
				var rawValue = nestedSource.UniqueContentEntry?.RawValue;

				if (Property.Parent.ValueEntry.WeakSmartValue is not IContentReference reference)
					return;

				var originEnable = GUI.enabled;
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
					GUI.enabled = false;
					EditorGUILayout.ObjectField
					(
						new GUIContent(string.Empty, tooltip: "Source"),
						_targetObject,
						nestedSource.Source.GetType(),
						false, GUILayout.Width(halfWidth)
					);
					GUI.enabled = originEnable;

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
									_targetToTree.tree = PropertyTree.Create(rawValue);
									_targetToTree.hash = hash;
								}

								_targetToTree.tree.Draw(false);

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
				if (source is IUniqueContentEntrySource unique)
				{
					Property.ValueEntry.WeakSmartValue = _mode switch
					{
						ContentDrawerMode.String => unique.Id,
						ContentDrawerMode.Guid or ContentDrawerMode.Reference => unique.UniqueContentEntry.Guid,
						_ => Property.ValueEntry.WeakSmartValue
					};
				}

				else
				{
					Property.ValueEntry.WeakSmartValue = _mode switch
					{
						ContentDrawerMode.String => string.Empty,
						ContentDrawerMode.Guid or ContentDrawerMode.Reference => SerializableGuid.Empty,
						_ => default
					};
				}
			}

			EditorGUI.indentLevel = originalIndent;
			GUI.enabled = originEnabled;
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
			if (_keyToSearchResult.ContainsKey(type) && !_keyToSearchResult[type])
				return null;

			if (ContentEditorCache.TryGetSource(type, id, out var source))
			{
				_keyToSearchResult[type] = true;
				return source;
			}

			ContentEditorCache.Refresh();
			if (ContentEditorCache.TryGetSource(type, id, out var refreshed))
			{
				_keyToSearchResult[type] = true;
				return refreshed;
			}

			_keyToSearchResult[type] = false;
			return null;
		}

		private IContentEntrySource FindSelectedSource(Type type, in SerializableGuid guid)
		{
			if (_keyToSearchResult.ContainsKey(type) && !_keyToSearchResult[type])
				return null;

			if (ContentEditorCache.TryGetSource(type, in guid, out var source))
			{
				_keyToSearchResult[type] = true;
				return source;
			}

			ContentEditorCache.Refresh();
			if (ContentEditorCache.TryGetSource(type, in guid, out var refreshed))
			{
				_keyToSearchResult[type] = true;
				return refreshed;
			}

			_keyToSearchResult[type] = false;
			return null;
		}

		private IContentEntrySource FindSelectedSource(IContentReference reference)
		{
			var type = reference.GetType();
			if (_keyToSearchResult.ContainsKey(type) && !_keyToSearchResult[type])
				return null;

			if (ContentEditorCache.TryGetSource(reference, out var source))
			{
				_keyToSearchResult[type] = true;
				return source;
			}

			ContentEditorCache.Refresh();
			if (ContentEditorCache.TryGetSource(reference, out var refreshedSource))
			{
				_keyToSearchResult[type] = true;
				return refreshedSource;
			}

			_keyToSearchResult[type] = false;
			return null;
		}
	}
}
