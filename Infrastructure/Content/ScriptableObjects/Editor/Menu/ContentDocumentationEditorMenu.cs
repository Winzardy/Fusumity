#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	public static class ContentDocumentationEditorMenu
	{
		private const string DOC_URL =
			"https://www.notion.so/winzardy/Content-Management-fafbd25804c44764a67913508ef158ff?pvs=4";

		[MenuItem(ContentMenuConstants.TOOLS_MENU + "\ud83d\uddc2\ufe0f Documentation", priority = 0)]
		public static void OpenDocumentation() => Application.OpenURL(DOC_URL);
	}
}
#endif
