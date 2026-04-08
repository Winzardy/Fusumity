using System;
using Content.Editor;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class ContentTechDescriptionEditModeAttribute : Attribute
	{
	}

	public sealed class ContentTechDescriptionEditModeAttributeDrawer : OdinAttributeDrawer<ContentTechDescriptionEditModeAttribute, string>
	{
		private const string ADD_BUTTON_LABEL = "Add";
		private const float ICON_SIZE = 12f;
		private const float BUTTON_SIZE = 13f;
		private const float BUTTON_MARGIN = 5f;

		private Rect? _cachedButtonRect;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (Property.SerializationRoot.ValueEntry.WeakSmartValue is not ContentScriptableObject scriptableObject)
			{
				CallNextDrawer(label);
				return;
			}

			var isDebugMode = ContentEntryDebugModeMenu.IsEnable;
			var isEditMode = scriptableObject.UseTechDescriptionEditor;
			var hasDescription = !ValueEntry.SmartValue.IsNullOrEmpty();
			var shouldShowEditor = hasDescription || isDebugMode && isEditMode;

			CacheButtonRect();

			if (!shouldShowEditor)
			{
				if (isDebugMode)
					DrawCollapsedRow(label);

				return;
			}

			if (isDebugMode && _cachedButtonRect.HasValue)
				DrawButton(_cachedButtonRect.Value, isEditMode);

			var canEdit = isDebugMode && isEditMode;
			var originalEnabled = GUI.enabled;
			GUI.enabled = originalEnabled && canEdit;
			CallNextDrawer(label);
			GUI.enabled = originalEnabled;

			if (isDebugMode && _cachedButtonRect.HasValue)
				DrawIcon(_cachedButtonRect.Value, isEditMode);
		}

		private void CacheButtonRect()
		{
			var rect = Property.LastDrawnValueRect;
			if (rect is {x: 0f, y: 0f})
				return;

			_cachedButtonRect = BuildButtonRect(rect);
		}

		private void DrawCollapsedRow(GUIContent label)
		{
			var rect = EditorGUILayout.GetControlRect();
			var buttonLabel = label?.text ?? Property.NiceName;
			var buttonRect = EditorGUI.PrefixLabel(rect, new GUIContent(buttonLabel));

			GUIHelper.PushGUIEnabled(true);
			{
				if (GUI.Button(buttonRect, ADD_BUTTON_LABEL, SirenixGUIStyles.MiniButton))
					ToggleEditMode(true);
			}
			GUIHelper.PopGUIEnabled();
		}

		private void DrawButton(Rect buttonRect, bool isEditMode)
		{
			EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);

			GUIHelper.PushGUIEnabled(true);
			{
				if (GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
					ToggleEditMode(!isEditMode);
			}
			GUIHelper.PopGUIEnabled();
		}

		private static Rect BuildButtonRect(Rect rect)
		{
			return new Rect(
				rect.xMax - BUTTON_SIZE - BUTTON_MARGIN + 1,
				rect.y + BUTTON_MARGIN + EditorGUIUtility.singleLineHeight - 2.5f,
				BUTTON_SIZE,
				BUTTON_SIZE);
		}

		private static void DrawIcon(Rect buttonRect, bool isEditMode)
		{
			var color = SirenixGUIStyles.IconButton.normal.textColor;
			if (!isEditMode)
				color = new Color(color.r, color.g, color.b, 0.45f);

			var iconOffset = (BUTTON_SIZE - ICON_SIZE) * 0.5f;
			var iconRect = new Rect(
				buttonRect.x + iconOffset,
				buttonRect.y + iconOffset,
				ICON_SIZE,
				ICON_SIZE);

			SdfIcons.DrawIcon(iconRect, isEditMode ? SdfIconType.PencilFill : SdfIconType.Pencil, color);
		}

		private void ToggleEditMode(bool enabled)
		{
			Property.RecordForUndo(enabled ? "Enable tech description edit mode" : "Disable tech description edit mode");

			foreach (var weakTarget in Property.Tree.WeakTargets)
			{
				if (weakTarget is not ContentScriptableObject scriptableObject)
					continue;

				scriptableObject.SetUseTechDescriptionEditor(enabled);
				EditorUtility.SetDirty(scriptableObject);
			}

			GUIHelper.RequestRepaint();
		}
	}
}
