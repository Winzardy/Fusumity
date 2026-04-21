using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace UI.Editor
{
	[Overlay(typeof(SceneView), "Layout Animations", "Layout Animations")]
	public class UIBaseLayoutEditorOverlay : IMGUIOverlay, ITransientOverlay
	{
		private const float MIN_OVERLAY_WIDTH = 235f;
		private const float ACTION_BUTTON_WIDTH = 74f;
		private const float ACTION_MENU_BUTTON_WIDTH = 18f;
		private const float CONTROL_SPACING = 4f;

		private UIBaseLayout _target;

		public bool visible { get => _target && _target.UseLayoutAnimations && !Application.isPlaying && HasKeys(_target); }

		public override void OnCreated()
		{
			Selection.selectionChanged += OnSelectionChanged;
			OnSelectionChanged();
		}

		public override void OnWillBeDestroyed()
		{
			Selection.selectionChanged -= OnSelectionChanged;
			_target = null;
		}

		public override void OnGUI()
		{
			if (!_target)
			{
				OnSelectionChanged();
				if (!_target)
					return;
			}

			GUILayoutUtility.GetRect(MIN_OVERLAY_WIDTH, 0f, GUILayout.ExpandWidth(true));
			EnsureSelectedKey();
			var keys = GetKeys();
			if (keys.Length == 0)
				return;

			DrawToolbar(keys);
		}

		private void OnSelectionChanged()
		{
			_target = null;

			if (Selection.activeGameObject == null)
				return;

			_target = Selection.activeGameObject.GetComponentInParent<UIBaseLayout>(true);
		}

		private static bool HasKeys(UIBaseLayout layout)
		{
			if (!layout || layout.customSequences == null)
				return false;

			foreach (var item in layout.customSequences)
			{
				if (!string.IsNullOrEmpty(item.key))
					return true;
			}

			return false;
		}

		private string[] GetKeys()
		{
			if (_target?.customSequences == null)
				return System.Array.Empty<string>();

			var count = 0;
			foreach (var item in _target.customSequences)
			{
				if (!string.IsNullOrEmpty(item.key))
					count++;
			}

			if (count == 0)
				return System.Array.Empty<string>();

			var keys = new string[count];
			var index = 0;
			foreach (var item in _target.customSequences)
			{
				if (string.IsNullOrEmpty(item.key))
					continue;

				keys[index++] = item.key;
			}

			return keys;
		}

		private void EnsureSelectedKey()
		{
			if (_target == null)
				return;

			if (TryGetSelectedSequence(_target.debugCurrentKey, out _))
				return;

			var keys = GetKeys();
			_target.debugCurrentKey = keys.Length > 0 ? keys[0] : null;
		}

		private static int GetSelectedIndex(string[] keys, string currentKey)
		{
			for (int i = 0; i < keys.Length; i++)
			{
				if (keys[i] == currentKey)
					return i;
			}

			return keys.Length > 0 ? 0 : -1;
		}

		private void DrawToolbar(string[] keys)
		{
			var rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
			var popupWidth = Mathf.Max(80f, rowRect.width - ACTION_BUTTON_WIDTH - CONTROL_SPACING);
			var popupRect = new Rect(rowRect.x, rowRect.y, popupWidth, rowRect.height);
			var buttonRect = new Rect(popupRect.xMax + CONTROL_SPACING, rowRect.y, ACTION_BUTTON_WIDTH, rowRect.height);

			var index = GetSelectedIndex(keys, _target.debugCurrentKey);
			var nextIndex = EditorGUI.Popup(popupRect, index, keys, EditorStyles.popup);
			if (nextIndex >= 0 && nextIndex < keys.Length)
				_target.debugCurrentKey = keys[nextIndex];

			var hasSelectedSequence = TryGetSelectedSequence(_target.debugCurrentKey, out var sequence);
			var canStop = hasSelectedSequence && sequence.EditorPreviewActive;
			var buttonLabel = canStop ? "Stop" : "Play";

			using (new EditorGUI.DisabledScope(!hasSelectedSequence))
			{
				var actionRect = buttonRect;
				actionRect.width -= ACTION_MENU_BUTTON_WIDTH;

				var dropdownRect = buttonRect;
				dropdownRect.xMin = actionRect.xMax;

				if (GUI.Button(actionRect, buttonLabel, EditorStyles.miniButtonLeft))
				{
					if (canStop)
						_target.StopAnimation(_target.debugCurrentKey, _target.debugPreviewReset);
					else
						_target.PlayAnimation(_target.debugCurrentKey, _target.debugPreviewReset, _target.debugPreviewLoop);
				}

				if (GUI.Button(dropdownRect, EditorGUIUtility.IconContent("_Popup"), EditorStyles.miniButtonRight))
					ShowPreviewMenu(buttonRect);
			}
		}

		private void ShowPreviewMenu(Rect rect)
		{
			var menu = new GenericMenu();
			menu.AddItem(new GUIContent("Reset"), _target.debugPreviewReset, () => _target.debugPreviewReset = !_target.debugPreviewReset);
			menu.AddItem(new GUIContent("Loop"), _target.debugPreviewLoop, () => _target.debugPreviewLoop = !_target.debugPreviewLoop);
			menu.DropDown(rect);
		}

		private bool TryGetSelectedSequence(string key, out ZenoTween.SequenceParticipant sequence)
		{
			sequence = null;

			if (_target?.customSequences == null || string.IsNullOrEmpty(key))
				return false;

			foreach (var item in _target.customSequences)
			{
				if (item.key != key)
					continue;

				sequence = item.sequence;
				return sequence != null;
			}

			return false;
		}
	}
}
