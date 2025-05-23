using System;

namespace Notifications
{
	public interface IPlatformNotification<T>
		where T : INotificationPlatform
	{
		public Type PlatformType { get; }
	}
}
