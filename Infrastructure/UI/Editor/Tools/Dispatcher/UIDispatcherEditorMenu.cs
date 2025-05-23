using UnityEditor;

namespace UI.Editor
{
	public class UIDispatcherEditorMenu
	{
		[MenuItem(GUIMenuConstants.TOOLS_MENU + "Dispatcher", priority = GUIMenuConstants.TOOLS_PRIORITY)]
		private static void OpenDispatcher()
			=> EditorWindow.GetWindow<UIDispatcherEditor>().Show();
	}
}
