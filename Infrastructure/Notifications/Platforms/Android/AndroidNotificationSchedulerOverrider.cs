namespace Notifications.Android
{
	public abstract class AndroidNotificationSchedulerOverrider<TScheduler> : BasePlatformNotificationSchedulerOverrider<TScheduler, AndroidNotificationPlatform>
		where TScheduler : NotificationScheduler
	{
	}
}
