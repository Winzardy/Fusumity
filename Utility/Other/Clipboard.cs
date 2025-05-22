namespace Fusumity.Utility
{
	using UnityClipboard = UnityEngine.GUIUtility;
	using UnityColor = UnityEngine.Color;
	using UnityDebug = UnityEngine.Debug;

	public static class Clipboard
	{
		public const string CHANNEL_FORMAT = "[{0}]";
		public static readonly UnityColor DEBUG_COLOR = UnityColor.gray;

		private const string CHANNEL_NAME = "Clipboard";

		private static readonly string PREFIX = string.Format(CHANNEL_FORMAT,
			CHANNEL_NAME.ColorTextInEditorOnly(DEBUG_COLOR));

		public static void Copy(string text)
		{
			UnityClipboard.systemCopyBuffer = text; //tvOS does not support
			UnityDebug.Log($"{PREFIX} text successfully copied: {text.UnderlineText()}");
		}

		public static string Paste()
		{
			return UnityClipboard.systemCopyBuffer;
		}

		public static void Paste(ref string text)
		{
			text = UnityClipboard.systemCopyBuffer;
		}
	}

	public static class ClipboardUtility
	{
		public static void CopyToClipboard(this string text) => Clipboard.Copy(text);
	}
}
