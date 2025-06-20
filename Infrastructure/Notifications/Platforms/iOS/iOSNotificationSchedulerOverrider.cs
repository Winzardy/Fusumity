namespace Notifications.iOS
{
	public abstract class iOSNotificationSchedulerOverrider<TScheduler> : BasePlatformNotificationSchedulerOverrider<TScheduler, iOSNotificationPlatform>
		where TScheduler : NotificationScheduler
	{
	}
}
