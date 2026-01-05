using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public static partial class FusumityEditorGUILayout
	{
		private const int BUTTON_PADDING = 4;
		private const int BUTTON_WIDTH = 100;
		private static GUIStyle _messageBoxWithButtonStyle;

		// TODO: сделать аттрибут [InfoBoxButton] (аналог [InfoBox]) который будет принимать условия показа и действия при клике
		public static bool MessageBoxButton(
			string message,
			string buttonLabel,
			MessageType boxType = MessageType.Warning,
			bool boxWide = true)
		{
			const int BUTTON_HEIGHT = 24;

			InitializeMessageBoxStyle();

			SirenixEditorGUI.MessageBox(message, boxType, _messageBoxWithButtonStyle, boxWide);

			var buttonRect = GUILayoutUtility.GetLastRect();
			buttonRect = buttonRect.AlignRight(BUTTON_WIDTH + BUTTON_PADDING);
			buttonRect.height = BUTTON_HEIGHT;
			buttonRect.x -= BUTTON_PADDING;
			buttonRect.y += BUTTON_PADDING;

			return GUI.Button(buttonRect, buttonLabel);
		}

		private static void InitializeMessageBoxStyle()
		{
			if (_messageBoxWithButtonStyle != null)
				return;

			_messageBoxWithButtonStyle = new GUIStyle(SirenixGUIStyles.MessageBox);

			_messageBoxWithButtonStyle.padding = new RectOffset
			{
				left = _messageBoxWithButtonStyle.padding.left + 1,
				right = _messageBoxWithButtonStyle.padding.right + BUTTON_WIDTH + BUTTON_PADDING + BUTTON_PADDING,
				top = _messageBoxWithButtonStyle.padding.top + 2,
				bottom = _messageBoxWithButtonStyle.padding.bottom + 2
			};

		}
	}
}
