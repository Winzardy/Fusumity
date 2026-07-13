using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using AssetManagement;
using Content;
using Localization;
using Sapientia.Extensions;
using Unity.Notifications.iOS;
using UnityEngine;
using UnityEngine.Scripting;

namespace Notifications.iOS
{
	public struct IOSPlatformNotificationRequest : IPlatformNotificationRequest
	{
		public string subtitle;
		public AssetReference<Sprite> attachment;
		public string category;
	}

	[Preserve]
	public class iOSNotificationPlatform : INotificationPlatform
	{
		private bool _accessGranted;

		public event Action<string, string> NotificationReceived;

		public iOSNotificationPlatform()
		{
			InitializeCategories();
			iOSNotificationCenter.OnNotificationReceived += OnNotificationReceived;
		}

		public async UniTask AuthorizeAsync(CancellationToken ct)
		{
			var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
			using (var req = new AuthorizationRequest(authorizationOption, false))
			{
				await UniTask.WaitUntil(() => req.IsFinished, cancellationToken: ct);

				_accessGranted = req.Granted;
			}

			if (_accessGranted)
				await IOSNotificationAttachmentCache.PrepareConfiguredAsync(ct);
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
			if (request.repeatInterval.HasValue)
			{
				NotificationsDebug.LogError(
					$"iOS does not support notification [ {request.id} ] with separate delivery time and repeat interval");
				return false;
			}

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

			notification.Badge = 1;
			var attachment = default(AssetReference<Sprite>);
			var category = default(string);

			var notificationEntry = request.config;
			notification.ShowInForeground = notificationEntry.showInForeground;
			if (notification.ShowInForeground)
				notification.ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound | PresentationOption.Badge;

			if (notificationEntry.TryGet<IOSPlatformNotificationConfig>(out var platformEntry))
			{
				attachment = platformEntry.attachment;
				notification.Subtitle = GetLocalized(platformEntry.subtitleLocKey);

				if (!platformEntry.category.IsEmpty())
					category = platformEntry.category.ToId();
			}

			if (request.TryGet<IOSPlatformNotificationRequest>(out var platformArgs))
			{
				if (!platformArgs.subtitle.IsNullOrEmpty())
					notification.Subtitle = platformArgs.subtitle;

				if (!platformArgs.attachment.IsEmptyOrInvalid())
					attachment = platformArgs.attachment;

				if (!platformArgs.category.IsNullOrEmpty())
					category = platformArgs.category;
			}

			notification.CategoryIdentifier = category;
			IOSNotificationAttachmentCache.Apply(notification, attachment);

			iOSNotificationCenter.ScheduleNotification(notification);
			return true;
		}

		public void Cancel(string id) => iOSNotificationCenter.RemoveScheduledNotification(id);

		public void CancelAll() => iOSNotificationCenter.RemoveAllScheduledNotifications();

		public void Remove(string id)
		{
			iOSNotificationCenter.RemoveDeliveredNotification(id);
			SetBadge(0);
		}

		public void RemoveAll()
		{
			iOSNotificationCenter.RemoveAllDeliveredNotifications();
			SetBadge(0);
		}

		public void OpenApplicationSettings() => iOSNotificationCenter.OpenNotificationSettings();

		public string GetLastIntentNotificationId()
		{
			var operation = iOSNotificationCenter.QueryLastRespondedNotification();
			return operation.keepWaiting ? null : operation.Notification?.Identifier;
		}

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
					deliveryTime = GetDeliveryTime(notification.Trigger),
					repeatInterval = GetRepeatInterval(notification.Trigger)
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

		private static TimeSpan? GetRepeatInterval(iOSNotificationTrigger trigger)
		{
			if (trigger is iOSNotificationTimeIntervalTrigger { Repeats: true } interval)
				return interval.TimeInterval;

			return null;
		}

		private static void InitializeCategories()
		{
			var categories = new List<iOSNotificationCategory>();
			var actionIds = new HashSet<string>();

			foreach (var (id, contentEntry) in ContentManager.GetAllEntries<IOSNotificationCategoryConfig>())
			{
				if (id.IsNullOrEmpty())
					continue;

				IOSNotificationCategoryConfig config = contentEntry;
				var category = new iOSNotificationCategory(id)
				{
					Options = config.options,
					HiddenPreviewsBodyPlaceholder = GetLocalized(config.hiddenPreviewsBodyPlaceholderLocKey),
					SummaryFormat = GetLocalized(config.summaryFormatLocKey)
				};

				if (config.intentIdentifiers != null)
				{
					foreach (var intentIdentifier in config.intentIdentifiers)
					{
						if (!intentIdentifier.IsNullOrEmpty())
							category.AddIntentIdentifier(intentIdentifier);
					}
				}

				if (config.actions != null)
				{
					foreach (var actionConfig in config.actions)
					{
						var action = CreateAction(actionConfig, id, actionIds);
						if (action != null)
							category.AddAction(action);
					}
				}

				categories.Add(category);
			}

			iOSNotificationCenter.SetNotificationCategories(categories);
		}

		private static iOSNotificationAction CreateAction(
			IOSNotificationActionConfig config,
			string categoryId,
			ISet<string> actionIds)
		{
			if (config == null || config.id.IsNullOrEmpty())
			{
				NotificationsDebug.LogError($"iOS notification category [ {categoryId} ] contains action with empty id");
				return null;
			}

			if (!actionIds.Add(config.id))
			{
				NotificationsDebug.LogError($"iOS notification action id [ {config.id} ] must be unique within the application");
				return null;
			}

			var title = GetLocalized(config.titleLocKey, config.id);
			iOSNotificationAction action;

			if (config.type == IOSNotificationActionType.TextInput)
			{
				var buttonTitle = GetLocalized(config.textInputButtonTitleLocKey, title);
				action = new iOSTextInputNotificationAction(config.id, title, config.options, buttonTitle)
				{
					TextInputPlaceholder = GetLocalized(config.textInputPlaceholderLocKey)
				};
			}
			else
			{
				action = new iOSNotificationAction(config.id, title, config.options);
			}

			if (!config.icon.IsNullOrEmpty())
			{
				switch (config.iconType)
				{
					case IOSNotificationActionIconType.SystemSymbol:
						action.SystemImageName = config.icon;
						break;
					case IOSNotificationActionIconType.AppTemplate:
						action.TemplateImageName = config.icon;
						break;
				}
			}

			return action;
		}

		private static string GetLocalized(string locKey, string fallback = null) =>
			locKey.IsNullOrEmpty() ? fallback : LocManager.Get(locKey);

		private void SetBadge(int amount) => iOSNotificationCenter.ApplicationBadge = amount;
	}
}
