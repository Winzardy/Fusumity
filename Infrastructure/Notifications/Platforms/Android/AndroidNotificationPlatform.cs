using System;
using Content;
using Localization;
using Notifications.Android.Config;
using Sapientia.Collections;
using Sapientia.Extensions;
using Unity.Notifications.Android;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Scripting;

namespace Notifications.Android
{
	public class AndroidPlatformNotificationArgs : IPlatformNotificationArgs
	{
		public string largeIcon;
		public string smallIcon;
		public Color? smallIconColor;
	}

	[Preserve]
	public class AndroidNotificationPlatform : INotificationPlatform
	{
		private const string PERMISSION = "android.permission.POST_NOTIFICATIONS";

		private BidirectionalMap<string, int> _ids;

		public event Action<string, string> NotificationReceived;

		public AndroidNotificationPlatform()
		{
			_ids = new();
			InitializeChannels();
			AndroidNotificationCenter.OnNotificationReceived += OnNotificationReceived;

			NotificationsDebug.Log($"[Android] User Permission: {AndroidNotificationCenter.UserPermissionToPost}");
		}

		public void Dispose()
		{
			_ids = null;
			AndroidNotificationCenter.OnNotificationReceived -= OnNotificationReceived;
		}

		private void OnNotificationReceived(AndroidNotificationIntentData data)
		{
			if (_ids.TryGetValue(data.Id, out var id))
				NotificationReceived?.Invoke(id, data.Notification.IntentData);
			else
				NotificationReceived?.Invoke(data.Notification.IntentData, data.Notification.IntentData);
			//TODO: как вариант записывать string айди в IntentData. Так же могут быть нотификации не "игровые"
		}

		public bool Schedule(in NotificationArgs args)
		{
			TryRequestUserPermission();

			var notification = new AndroidNotification(args.title, args.message, args.deliveryTime!.Value);

			//TODO: Важно отметить что IntentData используется как контейнер для хранения айди, возможно надо будет это убрать
			notification.IntentData = args.id;

			var notificationEntry = args.config;
			notification.ShowInForeground = notificationEntry.showInForeground;

			var channel = AndroidNotificationChannelType.DEFAULT;

			if (args.repeatInterval.HasValue)
				notification.RepeatInterval = args.repeatInterval.Value;

			if (notificationEntry.TryGet<AndroidPlatformNotificationConfig>(out var platformEntry))
			{
				if (!platformEntry.channel.IsEmpty())
					channel = platformEntry.channel.ToId();

				if (platformEntry.smallIcon != null)
					notification.SmallIcon = platformEntry.smallIcon;

				if (platformEntry.largeIcon != null)
					notification.LargeIcon = platformEntry.largeIcon;

				if (platformEntry.useCustomSmallIconColor)
					notification.Color = platformEntry.smallIconColor;
			}

			if (args.TryGet<AndroidPlatformNotificationArgs>(out var platformArgs))
			{
				if (platformArgs.smallIcon != null)
					notification.SmallIcon = platformArgs.smallIcon;

				if (platformArgs.largeIcon != null)
					notification.LargeIcon = platformArgs.largeIcon;

				if (platformArgs.smallIconColor.HasValue)
					notification.Color = platformArgs.smallIconColor;
			}

			if (_ids.TryGetValue(args.id, out var androidId))
				AndroidNotificationCenter.SendNotificationWithExplicitID(notification, channel, androidId);
			else
				_ids[args.id] = AndroidNotificationCenter.SendNotification(notification, channel);

			return true;
		}

		private static void TryRequestUserPermission()
		{
			if (AndroidNotificationCenter.UserPermissionToPost == PermissionStatus.Allowed)
				return;

			if (!Permission.HasUserAuthorizedPermission(PERMISSION))
				Permission.RequestUserPermission(PERMISSION);
		}

		public void Cancel(string id)
		{
			if (_ids.TryGetValue(id, out var androidId))
				AndroidNotificationCenter.CancelScheduledNotification(androidId);
		}

		public void CancelAll()
		{
			AndroidNotificationCenter.CancelAllScheduledNotifications();
		}

		public void Remove(string id)
		{
			if (_ids.TryGetValue(id, out var androidId))
				AndroidNotificationCenter.CancelDisplayedNotification(androidId);
		}

		public void RemoveAll()
		{
			AndroidNotificationCenter.CancelAllDisplayedNotifications();
		}

		public void OpenApplicationSettings() => AndroidNotificationCenter.OpenNotificationSettings();
		public string GetLastIntentNotificationId() => AndroidNotificationCenter.GetLastNotificationIntent()?.Notification.IntentData;

		private void InitializeChannels()
		{
			//TODO: есть еще какие-то AndroidNotificationChannelGroup...

			foreach (var (id, contentEntry) in ContentManager.GetAllEntries<AndroidNotificationChannelConfig>())
			{
				if (id.IsNullOrEmpty())
					continue;

				AndroidNotificationChannelConfig config = contentEntry;
				var name = config.nameLocKey.IsNullOrEmpty() ? id : LocManager.Get(config.nameLocKey);
				var description = config.descriptionLocKey.IsNullOrEmpty() ? name : LocManager.Get(config.descriptionLocKey);
				var channel = new AndroidNotificationChannel()
				{
					Id = id,

					Name = name,
					Description = description,

					CanShowBadge = config.canShowBadge,
					CanBypassDnd = config.canBypassDnd,
					EnableLights = config.enableLights,
					EnableVibration = config.enableVibration,
					//TODO: добавить какие-нибудь темплейты...
					VibrationPattern = config.vibrationPattern,
					Importance = config.importance,
					LockScreenVisibility = config.lockScreenVisibility,
				};

				AndroidNotificationCenter.RegisterNotificationChannel(channel);
			}
		}
	}
}
