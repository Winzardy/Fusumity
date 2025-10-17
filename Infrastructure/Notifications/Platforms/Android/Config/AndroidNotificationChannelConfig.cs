#if UNITY_EDITOR || UNITY_ANDROID
using Localization;
using Sirenix.OdinInspector;
using Unity.Notifications.Android;
#endif

namespace Notifications.Android.Config
{
	[System.Serializable]
	public class AndroidNotificationChannelConfig
	{
#if UNITY_ANDROID || UNITY_EDITOR
		/// <summary>
		/// Notification channel name which is visible to users.
		/// </summary>
		[LocKey]
		public string nameLocKey;

		/// <summary>
		/// User visible description of the notification channel.
		/// </summary>
		[LocKey]
		public string descriptionLocKey;

		// /// <summary>
		// /// The ID of the registered channel group this channel belongs to.
		// /// </summary>
		// public string group;

		/// <summary>
		/// Importance level which is applied to all notifications sent to the channel.
		/// This can be changed by users in the settings app. Android uses importance to determine how much the notification should interrupt the user (visually and audibly).
		/// The higher the importance of a notification, the more interruptive the notification will be.
		/// The possible importance levels are the following:
		///    High: Makes a sound and appears as a heads-up notification.
		///    Default: Makes a sound.
		///    Low: No sound.
		///    None: No sound and does not appear in the status bar.
		/// </summary>
		public Importance importance = Importance.Default;

		/// <summary>
		/// Whether or not notifications posted to this channel can bypass the Do Not Disturb.
		/// This can be changed by users in the settings app.
		/// </summary>
		public bool canBypassDnd;

		/// <summary>
		/// Whether notifications posted to this channel can appear as badges in a Launcher application.
		/// </summary>
		public bool canShowBadge;

		/// <summary>
		/// Sets whether notifications posted to this channel should display notification lights, on devices that support that feature.
		/// This can be changed by users in the settings app.
		/// </summary>/
		public bool enableLights;

		/// <summary>
		/// Sets whether notification posted to this channel should vibrate.
		/// This can be changed by users in the settings app.
		/// </summary>
		public bool enableVibration;

		/// <summary>
		/// Sets the vibration pattern for notifications posted to this channel.
		/// </summary>
		[ShowIf(nameof(enableVibration))]
		public long[] vibrationPattern;

		/// <summary>
		/// Sets whether or not notifications posted to this channel are shown on the lockscreen in full or redacted form.
		/// This can be changed by users in the settings app.
		/// </summary>
		public LockScreenVisibility lockScreenVisibility = LockScreenVisibility.Private;
#endif
	}
}
