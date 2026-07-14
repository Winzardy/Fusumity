#if UNITY_IOS || UNITY_EDITOR
using JetBrains.Annotations;
using AssetManagement;
using Content;
using Localization;
using Sapientia;
using Sirenix.OdinInspector;
using UnityEngine;
#endif

namespace Notifications.iOS
{
	[System.Serializable]
#if UNITY_IOS || UNITY_EDITOR
	[TypeRegistryItem("iOS", icon: SdfIconType.Apple)]
#endif
	public struct IOSPlatformNotificationConfig : IPlatformNotificationConfig
	{
#if UNITY_IOS || UNITY_EDITOR
		[InfoBox("Категория задаёт кнопки и отображение уведомления, но не управляет важностью как Android-канал\nЕсли не выбрана, уведомление отправляется без категории")]
		[CanBeEmpty]
		public ContentReference<IOSNotificationCategoryConfig> category;

		[InfoBox("Sprite будет сохранён как локальный PNG и переиспользован для следующих уведомлений")]
		public AssetReference<Sprite> attachment;

		[CanBeNull]
		[LocKey]
		public string subtitleLocKey;
#endif
	}
}
