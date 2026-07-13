#if UNITY_IOS || UNITY_EDITOR
using Localization;
using Sirenix.OdinInspector;
using Unity.Notifications.iOS;
#endif

namespace Notifications.iOS
{
	[System.Serializable]
	public class IOSNotificationCategoryConfig
	{
#if UNITY_IOS || UNITY_EDITOR
		public iOSNotificationCategoryOptions options;

		[LocKey]
		public string hiddenPreviewsBodyPlaceholderLocKey;

		[LocKey]
		public string summaryFormatLocKey;

		public string[] intentIdentifiers;
		public IOSNotificationActionConfig[] actions;
#endif
	}

	[System.Serializable]
	public class IOSNotificationActionConfig
	{
#if UNITY_IOS || UNITY_EDITOR
		public string id;

		[LocKey]
		public string titleLocKey;

		public iOSNotificationActionOptions options;
		public IOSNotificationActionType type;

		[ShowIf(nameof(type), IOSNotificationActionType.TextInput)]
		[LocKey]
		public string textInputButtonTitleLocKey;

		[ShowIf(nameof(type), IOSNotificationActionType.TextInput)]
		[LocKey]
		public string textInputPlaceholderLocKey;

		public IOSNotificationActionIconType iconType;

		[HideIf(nameof(iconType), IOSNotificationActionIconType.None)]
		public string icon;
#endif
	}
}
