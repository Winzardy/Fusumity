using UnityEditor;

namespace Content.Editor
{
	[InitializeOnLoad]
	public static class ContentEntryDebugModeMenu
	{
		public const string PATH = ContentMenuConstants.TOOLS_MENU + "Debug Mode";

		private static bool _enable;

		public static bool IsEnable => _enable;

		[MenuItem(PATH, priority = 1100)]
		private static void Toggle() => Toggle(!_enable);

		static ContentEntryDebugModeMenu()
		{
			_enable = EditorPrefs.GetBool(PATH, false);
			EditorApplication.delayCall += () => { Toggle(_enable); };
		}

		private static void Toggle(bool enabled)
		{
			Menu.SetChecked(PATH, enabled);
			EditorPrefs.SetBool(PATH, enabled);
			_enable = enabled;
			UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
		}
	}

}
