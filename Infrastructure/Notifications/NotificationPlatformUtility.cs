using Sapientia.Collections;

namespace Notifications
{
	public static class NotificationPlatformUtility
	{
		public static bool TryGet<T>(this NotificationConfig config, out T platformEntry)
			where T : IPlatformNotificationConfig
		{
			platformEntry = default;

			if (config.platformEntries.IsNullOrEmpty())
				return false;

			foreach (var x in config.platformEntries)
			{
				if (x is not T cast)
					continue;

				platformEntry = cast;
				return true;
			}

			return false;
		}

		public static bool TryGet<T>(this in NotificationArgs args, out T platformArgs)
			where T : IPlatformNotificationArgs
		{
			platformArgs = default;

			if (args.platform == null)
				return false;

			platformArgs = (T) args.platform;
			return true;
		}
	}
}
