using System;
using Sapientia.Collections;
using Unity.Notifications.iOS;
using UnityEngine.Scripting;

namespace Notifications.iOS
{
	public struct IOSPlatformNotificationArgs : IPlatformNotificationArgs
	{
		public string subtitle;
	}

	[Preserve]
	public class iOSNotificationPlatform : INotificationPlatform
	{
		/// <summary>
		/// Runtime коллекция, нужна чтобы пересобрать нотификации...
		/// </summary>
		private SimpleList<iOSNotification> _notifications = new();

		public event Action<string, string> NotificationReceived;

		public iOSNotificationPlatform()
		{
			iOSNotificationCenter.OnNotificationReceived += OnNotificationReceived;
		}

		public void Dispose()
		{
			iOSNotificationCenter.OnNotificationReceived -= OnNotificationReceived;
		}

		private void OnNotificationReceived(iOSNotification notification)
		{
			NotificationReceived?.Invoke(notification.Identifier, notification.Data);
		}

		public void Schedule(in NotificationArgs args)
		{
			var notification = new iOSNotification(args.id)
			{
				Title = args.title,
				Body = args.message,
				Badge = _notifications.Count,
			};

			var date = args.deliveryTime!.Value;
			notification.Trigger = new iOSNotificationCalendarTrigger
			{
				Year = date.Year,
				Month = date.Month,
				Day = date.Day,
				Hour = date.Hour,
				Minute = date.Minute,
				Second = date.Second
			};

			var notificationEntry = args.entry;
			notification.ShowInForeground = notificationEntry.showInForeground;

			if (args.TryGet<IOSPlatformNotificationArgs>(out var platformArgs))
				notification.Subtitle = platformArgs.subtitle;

			//TODO: добавить поддержку иконок через атачимент
			//Можно передавать картинки для iOS поместив их в StreamingAssets
			//"file://" + Path.Combine(Application.streamingAssetsPath, "pictureName.png")
			//var iconAttachment = new iOSNotificationAttachment();
			//notification.Attachments.Add(iconAttachment);

			iOSNotificationCenter.ScheduleNotification(notification);
			_notifications.Add(notification);
		}

		public void Cancel(string id) => iOSNotificationCenter.RemoveScheduledNotification(id);

		public void CancelAll() => iOSNotificationCenter.RemoveAllScheduledNotifications();

		public void Remove(string id) => iOSNotificationCenter.RemoveDeliveredNotification(id);
		public void RemoveAll() => iOSNotificationCenter.RemoveAllDeliveredNotifications();

		public void OpenApplicationSettings() => iOSNotificationCenter.OpenNotificationSettings();

		public string GetLastIntentNotificationId() => iOSNotificationCenter.QueryLastRespondedNotification()
		   .Notification?.Identifier;

		public void ClearAll()
		{
			CancelAll();
			RemoveAll();
			SetBadge(0);
		}

		private void SetBadge(int amount) => iOSNotificationCenter.ApplicationBadge = amount;
	}
}
