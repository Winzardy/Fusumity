using System;
using Sapientia.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Utility
{
	/// <summary>
	/// Ввод времени отдельными полями часов/минут/секунд — как в дровере SchedulePoint.
	/// Строкой «HH:mm» промахнуться легко, и её ещё надо разбирать и валидировать
	/// </summary>
	public class EditorTimeDialog : EditorWindow
	{
		private const float WIDTH = 300f;
		private const float HEIGHT = 128f;
		private const float FIELD_SPACING = 6f;

		private string _description;
		private string _confirmText;
		private Action<byte, byte, byte> _onConfirm;

		private int _hr;
		private int _min;
		private int _sec;

		private bool _focused;

		/// <summary>
		/// Модальный цикл нельзя запускать изнутри чужого OnGUI — редактор встаёт намертво,
		/// поэтому показ всегда уходит на конец кадра
		/// </summary>
		public static void Show(string title, TimeSpan value, Action<byte, byte, byte> onConfirm,
			string description = null, string confirmText = "Set")
		{
			EditorApplication.delayCall += () =>
			{
				var window = CreateInstance<EditorTimeDialog>();
				window.titleContent = new GUIContent(title);
				window._description = description;
				window._confirmText = confirmText;
				window._onConfirm = onConfirm;
				window._hr = value.Hours;
				window._min = value.Minutes;
				window._sec = value.Seconds;

				var main = EditorGUIUtility.GetMainWindowPosition();
				window.position = new Rect(main.center.x - WIDTH * 0.5f, main.center.y - HEIGHT * 0.5f, WIDTH, HEIGHT);
				window.ShowModalUtility();
			};
		}

		private void OnGUI()
		{
			if (!_description.IsNullOrEmpty())
				EditorGUILayout.LabelField(_description, EditorStyles.wordWrappedMiniLabel);

			using (new GUILayout.HorizontalScope())
			{
				GUI.SetNextControlName(nameof(EditorTimeDialog));
				_hr = Field("Hours", _hr, 23);
				GUILayout.Space(FIELD_SPACING);
				_min = Field("Minutes", _min, 59);
				GUILayout.Space(FIELD_SPACING);
				_sec = Field("Seconds", _sec, 59);
			}

			if (!_focused)
			{
				EditorGUI.FocusTextInControl(nameof(EditorTimeDialog));
				_focused = true;
			}

			HandleKeyboard();

			GUILayout.FlexibleSpace();

			using (new GUILayout.HorizontalScope())
			{
				GUILayout.Label($"{_hr:00}:{_min:00}:{_sec:00}", EditorStyles.miniLabel);
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Cancel", GUILayout.Width(80f)))
					Cancel();

				if (GUILayout.Button(_confirmText, GUILayout.Width(80f)))
					Confirm();
			}
		}

		private static int Field(string label, int value, int max)
		{
			using (new GUILayout.VerticalScope())
			{
				EditorGUILayout.LabelField(label, EditorStyles.miniLabel);

				// Клампим сразу при вводе: переполнение поля потом всё равно пришлось бы чинить
				return Mathf.Clamp(EditorGUILayout.IntField(value), 0, max);
			}
		}

		private void HandleKeyboard()
		{
			var e = Event.current;

			if (e.type != EventType.KeyDown)
				return;

			switch (e.keyCode)
			{
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					e.Use();
					Confirm();
					break;

				case KeyCode.Escape:
					e.Use();
					Cancel();
					break;
			}
		}

		private void Confirm()
		{
			var onConfirm = _onConfirm;
			var hr = (byte) _hr;
			var min = (byte) _min;
			var sec = (byte) _sec;
			_onConfirm = null;

			Close();

			// Обработчик — тоже после кадра: он обычно пишет в ассет, а мы всё ещё внутри
			// OnGUI закрываемого окна
			if (onConfirm != null)
				EditorApplication.delayCall += () => onConfirm(hr, min, sec);
		}

		private void Cancel()
		{
			_onConfirm = null;
			Close();
		}
	}
}
