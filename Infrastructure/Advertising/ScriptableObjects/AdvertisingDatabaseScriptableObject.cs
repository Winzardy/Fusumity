namespace Content.ScriptableObjects.Advertising
{
#if UNITY_EDITOR
	using UnityEditor;
	using Content.ScriptableObjects.Editor;

	public class Editor
	{
		private const string GROUP_NAME = "Advertising";
		private const string PATH = ContentMenuConstants.FULL_CREATE_MENU + GROUP_NAME + ContentMenuConstants.DATABASE_ITEM_NAME;

		[MenuItem(PATH, priority = ContentMenuConstants.DATABASE_PRIORITY)]
		public static void Create() => ContentDatabaseEditorUtility.Create<AdvertisingDatabaseScriptableObject>();
	}
#endif
	public class AdvertisingDatabaseScriptableObject : ContentDatabaseScriptableObject
	{
	}
}
