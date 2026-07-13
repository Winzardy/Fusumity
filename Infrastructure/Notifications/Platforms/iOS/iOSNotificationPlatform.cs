using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Notifications.iOS;
using UnityEngine.Scripting;

namespace Notifications.iOS
{
	public struct IOSPlatformNotificationRequest : IPlatformNotificationRequest
	{
		public string subtitle;
	}

	[Preserve]
	public class iOSNotificationPlatform : INotificationPlatform
	{
		private bool _accessGranted;
		private string _deviceToken;

		public event Action<string, string> NotificationReceived;

		public iOSNotificationPlatform()
		{
			iOSNotificationCenter.OnNotificationReceived += OnNotificationReceived;
		}

		public async UniTask AuthorizeAsync(CancellationToken ct)
		{
			var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge;
			using (var req = new AuthorizationRequest(authorizationOption, false))
			{
				await UniTask.WaitUntil(() => req.IsFinished, cancellationToken: ct);

				_accessGranted = req.Granted;
				_deviceToken = req.DeviceToken;
			}
		}

		public void Dispose()
		{
			iOSNotificationCenter.OnNotificationReceived -= OnNotificationReceived;
		}

		private void OnNotificationReceived(iOSNotification notification)
		{
			NotificationReceived?.Invoke(notification.Identifier, notification.Data);
		}

		public bool Schedule(in NotificationRequest request)
		{
			if (!_accessGranted)
				return false;

			var notification = new iOSNotification(request.id)
			{
				Title = request.title,
				Body = request.message
			};

			var date = request.deliveryTime!.Value;
			var isUtc = date.Kind == DateTimeKind.Utc;
			notification.Trigger = new iOSNotificationCalendarTrigger
			{
				Year = date.Year,
				Month = date.Month,
				Day = date.Day,
				Hour = date.Hour,
				Minute = date.Minute,
				Second = date.Second,
				Repeats = false,
				UtcTime = isUtc
			};

			//TODO: доработать кейс с бейджами (R&D)
			notification.Badge = 1;

			var notificationEntry = request.config;
			notification.ShowInForeground = notificationEntry.showInForeground;
			if (notification.ShowInForeground)
				notification.ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound | PresentationOption.Badge;

			if (request.TryGet<IOSPlatformNotificationRequest>(out var platformArgs))
				notification.Subtitle = platformArgs.subtitle;

			//	TODO: добавить поддержку иконок через атачимент
			//	Можно передавать картинки для iOS поместив их в StreamingAssets
			//	"file://" + Path.Combine(Application.streamingAssetsPath, "pictureName.png")
			//	var iconAttachment = new iOSNotificationAttachment();
			//	notification.Attachments.Add(iconAttachment);

			iOSNotificationCenter.ScheduleNotification(notification);
			return true;
		}

		public void Cancel(string id) => iOSNotificationCenter.RemoveScheduledNotification(id);

		public void CancelAll() => iOSNotificationCenter.RemoveAllScheduledNotifications();

		public void Remove(string id)
		{
			iOSNotificationCenter.RemoveDeliveredNotification(id);
			SetBadge(iOSNotificationCenter.GetDeliveredNotifications().Length);
		}

		public void RemoveAll()
		{
			iOSNotificationCenter.RemoveAllDeliveredNotifications();
			SetBadge(0);
		}

		public void OpenApplicationSettings() => iOSNotificationCenter.OpenNotificationSettings();

		public string GetLastIntentNotificationId() => iOSNotificationCenter.QueryLastRespondedNotification()
			.Notification?.Identifier;

		public IReadOnlyList<NotificationRequest> GetScheduledNotifications()
		{
			var notifications = iOSNotificationCenter.GetScheduledNotifications();
			var result = new NotificationRequest[notifications.Length];

			for (var i = 0; i < notifications.Length; i++)
			{
				var notification = notifications[i];
				result[i] = new NotificationRequest(notification.Identifier, default)
				{
					title = notification.Title,
					message = notification.Body,
					deliveryTime = GetDeliveryTime(notification.Trigger)
				};
			}

			return result;
		}

		private static DateTime? GetDeliveryTime(iOSNotificationTrigger trigger)
		{
			if (trigger is not iOSNotificationCalendarTrigger calendar ||
				!calendar.Year.HasValue ||
				!calendar.Month.HasValue ||
				!calendar.Day.HasValue ||
				!calendar.Hour.HasValue ||
				!calendar.Minute.HasValue ||
				!calendar.Second.HasValue)
				return null;

			var kind = calendar.UtcTime ? DateTimeKind.Utc : DateTimeKind.Local;
			return new DateTime(
				calendar.Year.Value,
				calendar.Month.Value,
				calendar.Day.Value,
				calendar.Hour.Value,
				calendar.Minute.Value,
				calendar.Second.Value,
				kind);
		}

		private void SetBadge(int amount) => iOSNotificationCenter.ApplicationBadge = amount;
	}
}
