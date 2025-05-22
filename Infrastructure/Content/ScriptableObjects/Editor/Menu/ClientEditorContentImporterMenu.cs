#if UNITY_EDITOR
using UnityEditor;

namespace Content.ScriptableObjects.Editor
{
	[InitializeOnLoad]
	public static class ClientEditorContentImporterMenu
	{
		public const string PATH = ContentMenuConstants.DATABASE_MENU + "Auto Sync On Play";

		private static bool _enable;

		public static bool IsEnable => _enable;

		[MenuItem(PATH, priority = 100)]
		private static void Toggle() => Toggle(!_enable);

		static ClientEditorContentImporterMenu()
		{
			_enable = EditorPrefs.GetBool(PATH, true);
			EditorApplication.delayCall += () => { Toggle(_enable); };
		}

		private static void Toggle(bool enabled)
		{
			Menu.SetChecked(PATH, enabled);
			EditorPrefs.SetBool(PATH, enabled);
			_enable = enabled;
		}
	}
}
#endif
