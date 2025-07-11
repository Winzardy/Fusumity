using System;
using Content;
using Localization;
using Notifications.Android.Entry;
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

		public void Schedule(in NotificationArgs args)
		{
			TryRequestUserPermission();

			var notification = new AndroidNotification(args.title, args.message, args.deliveryTime!.Value);

			//TODO: Важно отметить что IntentData используется как контейнер для хранения айди, возможно надо будет это убрать
			notification.IntentData = args.id;

			var notificationEntry = args.entry;
			notification.ShowInForeground = notificationEntry.showInForeground;

			var channel = AndroidNotificationChannelType.DEFAULT;

			if (args.repeatInterval.HasValue)
				notification.RepeatInterval = args.repeatInterval.Value;

			if (notificationEntry.TryGet<AndroidPlatformNotificationEntry>(out var platformEntry))
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
		}

		private static void TryRequestUserPermission()
		{
			if(AndroidNotificationCenter.UserPermissionToPost == PermissionStatus.Allowed)
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

			foreach (var (id, contentEntry) in ContentManager.GetAll<AndroidNotificationChannelEntry>())
			{
				if (id.IsNullOrEmpty())
					continue;

				AndroidNotificationChannelEntry entry = contentEntry;
				var name = entry.nameLocKey.IsNullOrEmpty() ? id : LocManager.Get(entry.nameLocKey);
				var description = entry.descriptionLocKey.IsNullOrEmpty() ? name : LocManager.Get(entry.descriptionLocKey);
				var channel = new AndroidNotificationChannel()
				{
					Id = id,

					Name = name,
					Description = description,

					CanShowBadge = entry.canShowBadge,
					CanBypassDnd = entry.canBypassDnd,
					EnableLights = entry.enableLights,
					EnableVibration = entry.enableVibration,
					//TODO: добавить какие-нибудь темплейты...
					VibrationPattern = entry.vibrationPattern,
					Importance = entry.importance,
					LockScreenVisibility = entry.lockScreenVisibility,
				};

				AndroidNotificationCenter.RegisterNotificationChannel(channel);
			}
		}
	}
}
