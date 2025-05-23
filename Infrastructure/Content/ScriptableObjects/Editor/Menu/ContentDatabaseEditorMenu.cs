using UnityEditor;

namespace Content.ScriptableObjects.Editor
{
	public static class ContentDatabaseEditorMenu
	{
		[MenuItem(ContentMenuConstants.DATABASE_MENU + "Sync All", priority = 1000)]
		public static void SyncAllDatabases()
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
	}
}
