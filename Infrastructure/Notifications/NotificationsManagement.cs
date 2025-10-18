using System;
using System.Collections.Generic;
using System.Linq;
using Fusumity.Reactive;
using Fusumity.Utility;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Reflection;

namespace Notifications
{
	public class NotificationsManagement : IDisposable
	{
		private const int DEFAULT_DELAY_MIN = 5;
		private const string SCHEDULED_LOG_MESSAGE_FORMAT = "Scheduled notification: {0}";
		private const string INVALID_ARGS_BY_TIME_LOGS_MESSAGE = "RemainingTime or deliveryTime can't be null at the same time";

		private readonly INotificationPlatform _platform;
		private readonly NotificationsSettings _settings;

		private List<NotificationScheduler> _registeredSchedulers;

		//Пока соотношение 1 к 1, но пока не придумал кейсов когда нужно больше одного overrider'а
		private Dictionary<Type, Type> _schedulerToOverrider;

		internal bool Active => _platform != null;
		internal Type PlatformType => _platform.GetType();

		public NotificationsManagement(NotificationsSettings settings, INotificationPlatform platform)
		{
			_settings = settings;
			_platform = platform;

			if (_platform == null)
				return;

			var platformName = _platform.GetType()
				.Name
				.Remove("NotificationPlatform");
			NotificationsDebug.Log($"Target platform: {platformName}");

			_platform.NotificationReceived += OnNotificationReceived;

			//Очищаем все уведомления при запуске и пересоздаем. Это решаем вопросы с призрачными уведомлениями
			if (!_settings.disableClearAllOnStart)
			{
				CancelAll();
				RemoveAll();
			}

			_schedulerToOverrider = ReflectionUtility.GetAllTypes<ISchedulerOverrider>(false)
				.Where(type => typeof(IPlatformNotification<>).MakeGenericType(PlatformType).IsAssignableFrom(type))
				.ToDictionary(
					type => type,
					type => type.GetInterface(typeof(ISchedulerOverrider<>).Name)!.GetGenericArguments().First()
				);

			UnityLifecycle.ApplicationFocusEvent += OnApplicationFocus;
		}

		public void Dispose()
		{
			if (_platform == null)
				return;

			_schedulerToOverrider = null;

			_platform.NotificationReceived -= OnNotificationReceived;

			UnityLifecycle.ApplicationFocusEvent -= OnApplicationFocus;
		}

		private void OnApplicationFocus()
		{
			var lastIntentNotificationId = GetLastIntentNotificationId();
			if (lastIntentNotificationId == null)
				return;

			Remove(lastIntentNotificationId);
		}

		internal bool TryCreateOrRegister(Type type, out NotificationScheduler scheduler)
		{
			scheduler = null;

			if (_settings.disableSchedulers.Contains(type.FullName))
				return false;

			scheduler = type.CreateInstance<NotificationScheduler>();

			ISchedulerOverrider overrider = null;
			if (_schedulerToOverrider.TryGetValue(type, out var overriderType))
				overrider = overriderType.CreateInstance<ISchedulerOverrider>();
			scheduler.Construct(overrider);

			_registeredSchedulers ??= new();
			_registeredSchedulers.Add(scheduler);

			return true;
		}

		internal void Schedule(ref NotificationArgs args)
		{
			if (!args.remainingTime.HasValue && !args.deliveryTime.HasValue)
				throw NotificationsDebug.Exception(INVALID_ARGS_BY_TIME_LOGS_MESSAGE);

			args.deliveryTime ??= DateTime.Now + args.remainingTime.Value;

			var date = args.deliveryTime!.Value;
			var isUtc = date.Kind == DateTimeKind.Utc;
			if (isUtc ? date <= DateTime.UtcNow : date <= DateTime.Now)
			{
				NotificationsDebug.LogError(
					$"Trying to schedule notification by id [ {args.id} ] in the past, " +
					$"date: {date.ToShortTimeString()}, {date.ToShortDateString()} (kind:{date.Kind})");
				return;
			}

			if (_platform.Schedule(in args))
				NotificationsDebug.Log(SCHEDULED_LOG_MESSAGE_FORMAT.Format(args));
		}

		internal void Cancel(string id) => _platform?.Cancel(id);

		internal void CancelAll() => _platform?.CancelAll();

		internal void Remove(string id) => _platform?.Remove(id);

		internal void RemoveAll() => _platform?.RemoveAll();

		internal void OpenApplicationSettings() => _platform?.OpenApplicationSettings();

		internal string GetLastIntentNotificationId() => _platform?.GetLastIntentNotificationId();

		private void OnNotificationReceived(string id, string data)
		{
			// TODO: Добавить по надобности Receivers..
		}
	}
}
