using System;
using Content;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public static class FusumityEditorGUIHelper
	{
		public static bool drawAssetReference = true;
	}

	public static partial class FusumityEditorGUILayout
	{
		public const string NONE = "None";

		/// <summary>
		/// Важно что эта просто боковая кнопка для поля, с этой Ping точкой...
		/// </summary>
		public static GUIStyle objectFieldButtonStyle = GUI.skin.FindStyle("ObjectFieldButton");

		public static bool ToolbarButton(Rect rect, EditorIcon icon, GUIStyle style = null,
			bool ignoreGUIEnabled = false)
		{
			if (style == null)
			{
				style = SirenixGUIStyles.ToolbarButton;
			}

			if (GUI.Button(rect, GUIContent.none, style))
			{
				GUIHelper.RemoveFocusControl();
				GUIHelper.RequestRepaint();
				return true;
			}

			if (Event.current.type == EventType.Repaint)
			{
				--rect.y;
				icon.Draw(rect, 16f);
			}

			if (!ignoreGUIEnabled ||
			    Event.current.button != 0 ||
			    Event.current.rawType != EventType.MouseDown ||
			    !GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
			{
				return false;
			}

			GUIHelper.RemoveFocusControl();
			GUIHelper.RequestRepaint();
			GUIHelper.PushGUIEnabled(true);
			Event.current.Use();
			GUIHelper.PopGUIEnabled();
			return true;
		}

		public static void FoldoutContainer(Func<Rect> header, Action body, ref bool foldout, object fadeGroupKey, bool useIndent = false)
		{
			var originalIndent = EditorGUI.indentLevel;

			if (!EditorGUIUtility.hierarchyMode)
				EditorGUI.indentLevel += 1;

			var rect = header();

			if (body == null)
				return;

			var position = rect.AlignBottom(EditorGUIUtility.singleLineHeight);
			position.width = SirenixEditorGUI.FoldoutWidth;
			if (!EditorGUIUtility.hierarchyMode)
			{
				var offset = SirenixEditorGUI.FoldoutWidth + 3;
				position.x -= offset;
				position.width += offset;
			}

			foldout = SirenixEditorGUI.Foldout(position, foldout, GUIContent.none);
			if (SirenixEditorGUI.BeginFadeGroup(fadeGroupKey, foldout))
			{
				using (new GUILayout.VerticalScope())
				{
					var originalForceHideMonoScriptInEditor = OdinEditor.ForceHideMonoScriptInEditor;
					OdinEditor.ForceHideMonoScriptInEditor = false;
					var originalDrawAssetReference = FusumityEditorGUIHelper.drawAssetReference;
					FusumityEditorGUIHelper.drawAssetReference = false;
					{
						if (useIndent)
							EditorGUI.indentLevel++;
						body();
						if (useIndent)
							EditorGUI.indentLevel--;
					}
					FusumityEditorGUIHelper.drawAssetReference = originalDrawAssetReference;
					OdinEditor.ForceHideMonoScriptInEditor = originalForceHideMonoScriptInEditor;
				}
			}

			SirenixEditorGUI.EndFadeGroup();

			EditorGUI.indentLevel = originalIndent;
		}

		public static SerializableGuid DrawGuidField(in SerializableGuid guid, GUIContent label)
		{
			var rawMode = false;
			return DrawGuidField(guid, label, ref rawMode);
		}

		public static SerializableGuid DrawGuidField(in SerializableGuid guid, GUIContent label, ref bool rawMode)
		{
			const string CLIPBOARD_TOOLBAR_PREFIX = "Copy: ";

			var drawButton = guid != SerializableGuid.Empty;

			var originEnabled = GUI.enabled;

			var rect = EditorGUILayout.GetControlRect();
			var clipboardButtonRect = rect;
			var clipboardButtonRect2 = rect;
			var copiedText = rawMode ? guid.low.ToString() : guid.ToString();
			var clipboardToolbar = new GUIContent(string.Empty, null, $"{CLIPBOARD_TOOLBAR_PREFIX}{copiedText}");
			if (drawButton)
			{
				clipboardButtonRect = clipboardButtonRect.AlignRight(18);
				clipboardButtonRect.height -= 2;
				clipboardButtonRect.y += 1;
				clipboardButtonRect.x -= 0.5f;

				{
					var originColor = GUI.color;
					{
						GUI.color *= 0.7f;
						if (GUI.Button(clipboardButtonRect, clipboardToolbar, GUIStyle.none))
							Clipboard.Copy(copiedText);

						EditorGUIUtility.AddCursorRect(clipboardButtonRect, MouseCursor.Link);
					}
					GUI.color = originColor;
				}

				if (rawMode)
				{
					clipboardButtonRect2 = clipboardButtonRect;
					clipboardButtonRect2.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
					var originColor = GUI.color;
					{
						GUI.color *= 0.7f;
						if (GUI.Button(clipboardButtonRect2, GUIContent.none, GUIStyle.none))
							Clipboard.Copy(guid.high.ToString());

						EditorGUIUtility.AddCursorRect(clipboardButtonRect2, MouseCursor.Link);
					}
					GUI.color = originColor;
				}
			}

			var rawModeToolbar = new GUIContent(string.Empty, null, "Raw Mode");
			var rawModeButtonRect = clipboardButtonRect.AlignRight(drawButton ? 36 : 18);
			rawModeButtonRect.height -= 2;
			rawModeButtonRect.y += 0.5f;
			rawModeButtonRect.x -= 0.5f;
			if (!rawMode)
			{
				rawModeButtonRect.y += 1;
				rawModeButtonRect.x += 0.5f;
				rawModeButtonRect.width -= 0.5f;
				rawModeButtonRect.height -= 0.5f;
			}

			{
				var originColor = GUI.color;
				{
					GUI.color *= 0.7f;
					if (GUI.Button(rawModeButtonRect, rawModeToolbar, GUIStyle.none))
						rawMode = !rawMode;

					EditorGUIUtility.AddCursorRect(clipboardButtonRect, MouseCursor.Link);
				}
				GUI.color = originColor;
			}

			var input = SerializableGuid.Empty;
			if (rawMode)
			{
				var highLabelText = label != null
					? label.text.IsNullOrEmpty()
						? string.Empty
						: " "
					: string.Empty;

				var low = SirenixEditorFields.DelayedLongField(rect, label, guid.low);
				var high = SirenixEditorFields.DelayedLongField(highLabelText, guid.high);
				input = new SerializableGuid(low, high);
			}
			else
			{
				var text = SirenixEditorFields.DelayedTextField(rect, label,
					guid == SerializableGuid.Empty ? string.Empty : guid.ToString());

				if (Guid.TryParse(text, out var parsedGuid))
					input = parsedGuid;
				else if (!text.IsNullOrEmpty())
					Debug.LogError($"Can't [ {text} ] parse to guid: ");
			}

			GUI.enabled = true;
			if (drawButton)
			{
				if (GUI.Button(clipboardButtonRect, clipboardToolbar, SirenixGUIStyles.ToolbarButton))
					Clipboard.Copy(copiedText);

				clipboardButtonRect.width = 11;
				clipboardButtonRect.x += 3f;
				SdfIcons.DrawIcon(clipboardButtonRect, SdfIconType.Clipboard, SirenixGUIStyles.IconButton.normal.textColor);

				EditorGUIUtility.AddCursorRect(clipboardButtonRect, MouseCursor.Link);

				copiedText = guid.high.ToString();
				clipboardToolbar.tooltip = $"{CLIPBOARD_TOOLBAR_PREFIX}{copiedText}";
				if (rawMode)
				{
					if (GUI.Button(clipboardButtonRect2, clipboardToolbar, SirenixGUIStyles.ToolbarButton))
						Clipboard.Copy(guid.high.ToString());

					clipboardButtonRect2.width = 11;
					clipboardButtonRect2.x += 3f;

					SdfIcons.DrawIcon(clipboardButtonRect2, SdfIconType.Clipboard, SirenixGUIStyles.IconButton.normal.textColor);

					EditorGUIUtility.AddCursorRect(clipboardButtonRect2, MouseCursor.Link);
				}
			}

			if (GUI.Button(rawModeButtonRect, rawModeToolbar, GUIStyle.none))
				rawMode = !rawMode;

			rawModeButtonRect.width = 11;
			rawModeButtonRect.x += 3f;
			SdfIcons.DrawIcon(rawModeButtonRect, rawMode ? SdfIconType.EyeFill : SdfIconType.EyeSlashFill,
				SirenixGUIStyles.IconButton.normal.textColor);

			EditorGUIUtility.AddCursorRect(rawModeButtonRect, MouseCursor.Link);

			GUI.enabled = originEnabled;
			return input;
		}

		public static float GetHalfFieldWidth(bool useLabel = true)
		{
			var labelWidth = useLabel ? GUIHelper.BetterLabelWidth : 0;
			var width = GUIHelper.BetterContextWidth - labelWidth;
			width /= 2;
			width -= 21;
			return width;
		}

		public static void DrawWarning(string text,
			int topFlexibleSpace = 1,
			int bottomFlexibleSpace = 3,
			float iconSize = 48,
			GUIContent icon = null)
		{
			for (int i = 0; i < topFlexibleSpace; i++)
				GUILayout.FlexibleSpace();

			using (new EditorGUILayout.VerticalScope())
			{
				var style = new GUIStyle(EditorStyles.boldLabel)
				{
					alignment = TextAnchor.MiddleCenter,
					fontStyle = FontStyle.Bold,
					padding = new RectOffset(10, 10, 10, 10),
					fontSize = 20
				};

				icon ??= EditorGUIUtility.IconContent("console.warnicon");

				var iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.ExpandWidth(true));
				GUI.DrawTexture(iconRect, icon.image, ScaleMode.ScaleToFit);

				GUILayout.Label(text, style, GUILayout.ExpandWidth(true));
			}

			for (int i = 0; i < bottomFlexibleSpace; i++)
				GUILayout.FlexibleSpace();
		}

		public static void DrawPolymorphicObjectField()
		{
		}
	}
}
