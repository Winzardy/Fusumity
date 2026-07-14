using Sapientia.Collections;

namespace Notifications
{
	public static class NotificationPlatformUtility
	{
		public static bool TryGet<T>(this in NotificationConfig config, out T platformEntry)
			where T : IPlatformNotificationConfig
		{
			platformEntry = default;

			if (config.platformConfigs.IsNullOrEmpty())
				return false;

			foreach (var x in config.platformConfigs)
			{
				if (x is not T cast)
					continue;

				platformEntry = cast;
				return true;
			}

			return false;
		}

		public static bool TryGet<T>(this in NotificationRequest request, out T platformArgs)
			where T : IPlatformNotificationRequest
		{
			platformArgs = default;

			if (request.platform == null)
				return false;

			platformArgs = (T) request.platform;
			return true;
		}
	}
}
