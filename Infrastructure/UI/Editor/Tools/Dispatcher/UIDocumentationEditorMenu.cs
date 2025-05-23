using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
	public static class UIDocumentationEditorMenu
	{
		private const string DOC_URL =
			"https://www.notion.so/winzardy/UI-c995de9d7d474ec886af1388e80aea42?pvs=4";

		[MenuItem(GUIMenuConstants.TOOLS_MENU + "\ud83d\uddc2\ufe0f  Documentation", priority = 0)]
		public static void OpenDocumentation() => Application.OpenURL(DOC_URL);
	}
}
