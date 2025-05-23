#if UNITY_ANDROID || UNITY_EDITOR
using Content;
using UnityEngine;
using Unity.Notifications.Android;
using Sirenix.OdinInspector;
#endif

namespace Notifications.Android.Entry
{
	[System.Serializable]
	public struct AndroidPlatformNotificationEntry : IPlatformNotificationEntry
	{
#if UNITY_ANDROID || UNITY_EDITOR
		[InfoBox("Если не выбран, выбирается канал по дефолту")]
		public ContentReference<AndroidNotificationChannelEntry> channel;

		public NotificationStyle style;

		[Tooltip("'Large' иконка по умолчанию, так же иконка может быть выставлена программно.")]
		public string largeIcon;

		[Tooltip("'Small' иконка по умолчанию, так же иконка может быть выставлена программно.")]
		public string smallIcon;

		public bool useCustomSmallIconColor;

		[ShowIf(nameof(useCustomSmallIconColor))]
		public Color smallIconColor;
#endif
	}
}
