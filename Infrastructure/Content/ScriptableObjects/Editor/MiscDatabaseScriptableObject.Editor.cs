using UnityEditor;

namespace Content.ScriptableObjects.Editor
{
	public class Editor
	{
		private const string GROUP_NAME = "Misc";
		private const string PATH = ContentMenuConstants.FULL_CREATE_MENU + GROUP_NAME + ContentMenuConstants.DATABASE_ITEM_NAME;

		[MenuItem(PATH, priority = ContentMenuConstants.DATABASE_PRIORITY)]
		public static void Create() => ContentDatabaseEditorUtility.Create<MiscDatabaseScriptableObject>();
	}
}
