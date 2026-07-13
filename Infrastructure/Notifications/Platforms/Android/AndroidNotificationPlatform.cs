using System;
using System.Collections.Generic;
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
	public class AndroidPlatformNotificationRequest : IPlatformNotificationRequest
	{
		public string largeIcon;
		public string smallIcon;
		public Color? smallIconColor;
	}

	[Preserve]
	public class AndroidNotificationPlatform : INotificationPlatform
	{
		private const string PERMISSION = "android.permission.POST_NOTIFICATIONS";
		private const string NOTIFICATION_MANAGER_CLASS = "com.unity.androidnotifications.UnityNotificationManager";
		private const string EXTRA_TITLE = "android.title";
		private const string EXTRA_TEXT = "android.text";
		private const string EXTRA_ID = "id";
		private const string EXTRA_DATA = "data";
		private const string EXTRA_FIRE_TIME = "fireTime";
		private const string EXTRA_REPEAT_INTERVAL = "repeatInterval";

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

		public bool Schedule(in NotificationRequest request)
		{
			TryRequestUserPermission();

			var notification = new AndroidNotification(request.title, request.message, request.deliveryTime!.Value);

			//TODO: Важно отметить что IntentData используется как контейнер для хранения айди, возможно надо будет это убрать
			notification.IntentData = request.id;

			var notificationEntry = request.config;
			notification.ShowInForeground = notificationEntry.showInForeground;

			var channel = AndroidNotificationChannelType.DEFAULT;

			if (request.repeatInterval.HasValue)
				notification.RepeatInterval = request.repeatInterval.Value;

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

			if (request.TryGet<AndroidPlatformNotificationRequest>(out var platformArgs))
			{
				if (platformArgs.smallIcon != null)
					notification.SmallIcon = platformArgs.smallIcon;

				if (platformArgs.largeIcon != null)
					notification.LargeIcon = platformArgs.largeIcon;

				if (platformArgs.smallIconColor.HasValue)
					notification.Color = platformArgs.smallIconColor;
			}

			if (_ids.TryGetValue(request.id, out var androidId))
				AndroidNotificationCenter.SendNotificationWithExplicitID(notification, channel, androidId);
			else
				_ids[request.id] = AndroidNotificationCenter.SendNotification(notification, channel);

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

		public IReadOnlyList<NotificationRequest> GetScheduledNotifications()
		{
			var result = new List<NotificationRequest>();

			using var managerClass = new AndroidJavaClass(NOTIFICATION_MANAGER_CLASS);
			using var manager = managerClass.GetStatic<AndroidJavaObject>("mUnityNotificationManager");
			if (manager == null)
				return result;

			using var scheduledNotifications = manager.Get<AndroidJavaObject>("mScheduledNotifications");
			if (scheduledNotifications == null)
				return result;

			using var values = scheduledNotifications.Call<AndroidJavaObject>("values");
			using var iterator = values.Call<AndroidJavaObject>("iterator");
			while (iterator.Call<bool>("hasNext"))
			{
				using var builder = iterator.Call<AndroidJavaObject>("next");
				using var extras = builder.Call<AndroidJavaObject>("getExtras");

				var systemId = extras.Call<int>("getInt", EXTRA_ID, -1);
				var id = extras.Call<string>("getString", EXTRA_DATA) ?? systemId.ToString();
				var fireTime = extras.Call<long>("getLong", EXTRA_FIRE_TIME, -1L);
				var repeatInterval = extras.Call<long>("getLong", EXTRA_REPEAT_INTERVAL, -1L);

				result.Add(new NotificationRequest(id, default)
				{
					title = extras.Call<string>("getString", EXTRA_TITLE),
					message = extras.Call<string>("getString", EXTRA_TEXT),
					deliveryTime = ToDateTime(fireTime),
					repeatInterval = repeatInterval > 0 ? TimeSpan.FromMilliseconds(repeatInterval) : null
				});
			}

			return result;
		}

		private static DateTime? ToDateTime(long milliseconds)
		{
			if (milliseconds < 0)
				return null;

			return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
				.AddMilliseconds(milliseconds)
				.ToLocalTime();
		}

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
