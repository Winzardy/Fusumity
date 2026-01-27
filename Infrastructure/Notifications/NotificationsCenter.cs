using System;
using System.Runtime.CompilerServices;
using Sapientia;

namespace Notifications
{
	public class NotificationsCenter : StaticAccessor<NotificationsManagement>
	{
		private static NotificationsManagement management
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		public static bool IsInitialized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance != null;
		}

		public static bool Active => management.Active;
		public static Type PlatformType => management.PlatformType;

		public static bool Register<T>(T scheduler) where T : NotificationScheduler
			=> management.Register(scheduler);

		public static bool Unregister<T>(T scheduler) where T : NotificationScheduler
			=> management.Unregister(scheduler);

		public static void Schedule(ref NotificationRequest request) => management.Schedule(ref request);

		public static string GetLastIntentNotificationId() => management.GetLastIntentNotificationId();

		public static void OpenApplicationSettings() => management.OpenApplicationSettings();
	}
}
