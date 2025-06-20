using Sapientia.Collections;

namespace Notifications
{
	public static class NotificationExt
	{
		public static bool TryGet<T>(this NotificationEntry entry, out T platformEntry) where T : IPlatformNotificationEntry
		{
			platformEntry = default;

			if (entry.platforms.IsNullOrEmpty())
				return false;

			foreach (var x in entry.platforms)
			{
				if (x is not T cast)
					continue;

				platformEntry = cast;
				return true;
			}

			return false;
		}

		public static bool TryGet<T>(this in NotificationArgs args, out T platformArgs) where T : IPlatformNotificationArgs
		{
			platformArgs = default;

			if (args.platform == null)
				return false;

			platformArgs = (T) args.platform;
			return true;
		}
	}
}
