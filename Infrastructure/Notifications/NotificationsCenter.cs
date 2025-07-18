using System;
using System.Runtime.CompilerServices;
using Sapientia;

namespace Notifications
{
	public class NotificationsCenter : StaticProvider<NotificationsManagement>
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

		public static bool TryRegisterScheduler(Type type, out NotificationScheduler scheduler)
			=> management.TryRegisterScheduler(type, out scheduler);

		public static void Schedule(ref NotificationArgs args) => management.Schedule(ref args);

		public static string GetLastIntentNotificationId() => management.GetLastIntentNotificationId();
	}
}
