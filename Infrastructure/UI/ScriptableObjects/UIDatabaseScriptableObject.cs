using Sapientia;
using Sirenix.OdinInspector;
using UI;

namespace Content.ScriptableObjects.UI
{
	using UnityEngine;
#if UNITY_EDITOR
	using UnityEditor;
	using Content.ScriptableObjects.Editor;

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
#if DebugLog
		[TitleGroup("Debug")]
		[ShowInInspector]
		[SuffixLabel("ms", true)]
		[LabelText("Custom Layout Destroy Delay")]
		public Toggle<int> DebugLayoutDestroyDelay
		{
			get => UILayoutDebug.debugDelayMs;
			set => UILayoutDebug.debugDelayMs = value;
		}
#endif
		public Sprite placeholderSprite;
		public Color placeholderColor;
	}
}
