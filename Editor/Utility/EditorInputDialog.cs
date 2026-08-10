using System;
using Sapientia.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Utility
{
	/// <summary>
	/// Модальное окно с одним текстовым полем: EditorUtility.DisplayDialog ввода не умеет,
	/// а звать ради строки полноценный редактор ассета — лишний шаг
	/// </summary>
	public class EditorInputDialog : EditorWindow
	{
		private const float WIDTH = 320f;
		private const float HEIGHT = 104f;

		private string _label;
		private string _description;
		private string _value;
		private string _confirmText;
		private Action<string> _onConfirm;

		private bool _focused;

		/// <summary>
		/// Модальный цикл нельзя запускать изнутри чужого OnGUI — редактор встаёт намертво,
		/// поэтому показ всегда уходит на конец кадра. Звать можно откуда угодно, в том числе
		/// прямо из отрисовки дровера
		/// </summary>
		public static void Show(string title, string label, string value, Action<string> onConfirm,
			string description = null, string confirmText = "Add")
		{
			EditorApplication.delayCall += () =>
			{
				var window = CreateInstance<EditorInputDialog>();
				window.titleContent = new GUIContent(title);
				window._label = label;
				window._description = description;
				window._value = value ?? string.Empty;
				window._confirmText = confirmText;
				window._onConfirm = onConfirm;

				var main = EditorGUIUtility.GetMainWindowPosition();
				window.position = new Rect(main.center.x - WIDTH * 0.5f, main.center.y - HEIGHT * 0.5f, WIDTH, HEIGHT);
				window.ShowModalUtility();
			};
		}

		private void OnGUI()
		{
			if (!_description.IsNullOrEmpty())
				EditorGUILayout.LabelField(_description, EditorStyles.wordWrappedMiniLabel);

			GUI.SetNextControlName(nameof(EditorInputDialog));
			_value = EditorGUILayout.TextField(_label, _value);

			if (!_focused)
			{
				EditorGUI.FocusTextInControl(nameof(EditorInputDialog));
				_focused = true;
			}

			HandleKeyboard();

			GUILayout.FlexibleSpace();

			using (new GUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Cancel", GUILayout.Width(80f)))
					Cancel();

				using (new EditorGUI.DisabledScope(_value.IsNullOrEmpty()))
				{
					if (GUILayout.Button(_confirmText, GUILayout.Width(80f)))
						Confirm();
				}
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
					if (_value.IsNullOrEmpty())
						return;

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
			var value = _value;
			_onConfirm = null;

			Close();

			// Обработчик — тоже после кадра: он обычно пишет в ассет, а мы всё ещё внутри
			// OnGUI закрываемого окна
			if (onConfirm != null)
				EditorApplication.delayCall += () => onConfirm(value);
		}

		private void Cancel()
		{
			_onConfirm = null;
			Close();
		}
	}
}
