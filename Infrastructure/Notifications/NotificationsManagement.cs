using System;
using System.Collections.Generic;
using System.Linq;
using Fusumity.Utility;
using Sapientia.Collections;
using Sapientia.Reflection;

namespace Notifications
{
	public class NotificationsManagement : IDisposable
	{
		private const int DEFAULT_DELAY_MIN = 5;

		private readonly INotificationPlatform _platform;
		private readonly NotificationsSettings _settings;

		private List<NotificationScheduler> _schedulers;

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
		}

		public void Dispose()
		{
			if (_platform == null)
				return;

			_schedulerToOverrider = null;

			_platform.NotificationReceived -= OnNotificationReceived;

			if (!_schedulers.IsNullOrEmpty())
				foreach (var scheduler in _schedulers)
					scheduler.Dispose();

			_schedulers = null;
		}

		internal bool TryRegisterScheduler(Type type, out NotificationScheduler scheduler)
		{
			scheduler = null;

			if (_settings.disableSchedulers.Contains(type.FullName))
				return false;

			scheduler = type.CreateInstance<NotificationScheduler>();

			ISchedulerOverrider overrider = null;
			if (_schedulerToOverrider.TryGetValue(type, out var overriderType))
				overrider = overriderType.CreateInstance<ISchedulerOverrider>();
			scheduler.Construct(overrider);

			_schedulers ??= new();
			_schedulers.Add(scheduler);

			return true;
		}

		internal void Schedule(ref NotificationArgs args)
		{
			if (!args.remainingTime.HasValue && !args.deliveryTime.HasValue)
				throw new ArgumentException("RemainingTime or deliveryTime can't be null at the same time");

			args.deliveryTime ??= DateTime.Now + args.remainingTime.Value;

			_platform.Schedule(in args);

			NotificationsDebug.Log($"Schedule notification: {args}");
		}

		internal void Cancel(string id) => _platform?.Cancel(id);

		internal void CancelAll() => _platform?.CancelAll();

		internal void Remove(string id) => _platform?.Remove(id);

		internal void RemoveAll() => _platform?.RemoveAll();

		internal void OpenApplicationSettings() => _platform?.OpenApplicationSettings();

		internal string GetLastIntentNotificationId() => _platform?.GetLastIntentNotificationId();

		private void OnNotificationReceived(string id, string data)
		{
			//TODO: Добавить по надобности Receivers..
		}
	}
}
