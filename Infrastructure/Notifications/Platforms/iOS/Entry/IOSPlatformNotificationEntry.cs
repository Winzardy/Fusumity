#if UNITY_IOS || UNITY_EDITOR
using Localization;
#endif

namespace Notifications.iOS
{
	[System.Serializable]
	public class IOSPlatformNotificationEntry : IPlatformNotificationEntry
	{
#if UNITY_IOS || UNITY_EDITOR
		//TODO: добавить категории
		//Категории что-то наподобии каналов в Android

		//TODO: public string icon;
		//Иконки должны лежать в streamingAssets и передаваться как аттачмент

		[LocKey]
		public string subtitleLocKey;
#endif
	}
}
