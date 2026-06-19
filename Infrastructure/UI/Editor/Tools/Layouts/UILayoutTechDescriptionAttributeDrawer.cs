using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class UILayoutTechDescriptionAttribute : Attribute
	{
	}

	/// <summary>
	/// Рисует <see cref="UIBaseLayout.techDescription"/> read-only с иконкой-переключателем режима правки
	/// (карандаш → редактировать, крестик → зафиксировать). Включить правку у пустого поля можно через
	/// ContextMenu "Tech Description/Enable".
	/// </summary>
	public sealed class UILayoutTechDescriptionAttributeDrawer
		: OdinAttributeDrawer<UILayoutTechDescriptionAttribute, string>
	{
		private const float ICON_SIZE = 12f;
		private const float BUTTON_SIZE = 13f;
		private const float BUTTON_MARGIN = 5f;

		private Rect? _cachedButtonRect;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var layout = GetLayout();
			if (layout == null)
			{
				CallNextDrawer(label);
				return;
			}

			var isEditMode = layout.UseTechDescriptionEditor;
			var hasDescription = !string.IsNullOrEmpty(ValueEntry.SmartValue);

			// Пусто и режим выключен — поле скрыто (включается через ContextMenu "Tech Description/Enable").
			if (!hasDescription && !isEditMode)
				return;

			CacheButtonRect();

			if (_cachedButtonRect.HasValue)
				DrawButton(_cachedButtonRect.Value, isEditMode);

			// По умолчанию read-only; редактирование доступно только в режиме (карандаш / ContextMenu).
			var originalEnabled = GUI.enabled;
			GUI.enabled = originalEnabled && isEditMode;
			CallNextDrawer(label);
			GUI.enabled = originalEnabled;

			if (_cachedButtonRect.HasValue)
				DrawIcon(_cachedButtonRect.Value, isEditMode);
		}

		private UIBaseLayout GetLayout()
			=> Property.Parent?.ValueEntry?.WeakSmartValue as UIBaseLayout
				?? Property.SerializationRoot?.ValueEntry?.WeakSmartValue as UIBaseLayout;

		private void CacheButtonRect()
		{
			var rect = Property.LastDrawnValueRect;
			if (rect is {x: 0f, y: 0f})
				return;

			_cachedButtonRect = BuildButtonRect(rect);
		}

		private void DrawButton(Rect buttonRect, bool isEditMode)
		{
			EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);

			if (GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
				ToggleEditMode(!isEditMode);
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

			SdfIcons.DrawIcon(iconRect, isEditMode ? SdfIconType.XCircleFill : SdfIconType.Pencil, color);
		}

		private void ToggleEditMode(bool enabled)
		{
			Property.RecordForUndo(enabled ? "Enable tech description edit mode" : "Disable tech description edit mode");

			foreach (var weakTarget in Property.Tree.WeakTargets)
			{
				if (weakTarget is not UIBaseLayout layout)
					continue;

				layout.SetUseTechDescriptionEditor(enabled);
				EditorUtility.SetDirty(layout);
			}

			GUIHelper.RequestRepaint();
		}
	}
}
