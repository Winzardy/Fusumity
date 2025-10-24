#if UNITY_IOS || UNITY_EDITOR
using Localization;
using Sirenix.OdinInspector;
#endif

namespace Notifications.iOS
{
	[System.Serializable]
#if UNITY_IOS || UNITY_EDITOR
	[TypeRegistryItem("iOS", icon: SdfIconType.Apple)]
#endif
	public class IOSPlatformNotificationConfig : IPlatformNotificationConfig
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
