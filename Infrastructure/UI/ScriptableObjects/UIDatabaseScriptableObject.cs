namespace Content.ScriptableObjects.UI
{
#if UNITY_EDITOR
	using UnityEditor;
	using Content.ScriptableObjects.Editor;
	using UnityEngine;

	public class Editor
	{
		private const string GROUP_NAME = "UI";
		private const string PATH = ContentMenuConstants.FULL_CREATE_MENU + GROUP_NAME + ContentMenuConstants.DATABASE_ITEM_NAME;

		[MenuItem(PATH, priority = ContentMenuConstants.DATABASE_PRIORITY)]
		public static void Create() =>
			ContentDatabaseEditorUtility.Create<UIDatabaseScriptableObject>("GUIDatabase", GROUP_NAME);
	}
#endif
	public class UIDatabaseScriptableObject : ContentDatabaseScriptableObject
	{
		public Sprite placeholderSprite;
		public Color placeholderColor;
	}
}
