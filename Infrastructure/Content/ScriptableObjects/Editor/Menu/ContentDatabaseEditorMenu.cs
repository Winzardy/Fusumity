using UnityEditor;

namespace Content.ScriptableObjects.Editor
{
	public static class ContentDatabaseEditorMenu
	{
		public const int PRIORITY = 100;

		[MenuItem(ContentMenuConstants.DATABASE_MENU + "Sync All", priority = PRIORITY + 20)]
		public static void SyncAll()
		{
			var origin = ContentDebug.Logging.database;
			try
			{
				ContentDebug.Logging.database = true;
				ContentDatabaseEditorUtility.SyncContent();
			}
			finally
			{
				ContentDebug.Logging.database = origin;
			}
		}

		[MenuItem(ContentMenuConstants.DATABASE_MENU + "/Export", priority = PRIORITY + 40)]
		public static void Export() => ContentDatabaseExportEditorWindow.Open();
	}

	[InitializeOnLoad]
	public static class ClientEditorContentImporterMenu
	{
		public const string PATH = ContentMenuConstants.DATABASE_MENU + "Auto Sync On Play";

		private static bool _enable;

		public static bool IsEnable => _enable;

		[MenuItem(PATH, priority = ContentDatabaseEditorMenu.PRIORITY)]
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
