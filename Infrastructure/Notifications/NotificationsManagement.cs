using System;
using System.Collections.Generic;
using Fusumity.Reactive;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Reflection;
using Sapientia.Utility;

namespace Notifications
{
	public class NotificationsManagement : IDisposable
	{
		private const string SCHEDULED_LOG_MESSAGE_FORMAT = "Scheduled notification: {0}";
		private const string FAILED_LOG_MESSAGE_FORMAT = "Failed to schedule notification: {0}";
		private const string INVALID_ARGS_BY_TIME_LOGS_MESSAGE = "RemainingTime or deliveryTime can't be null at the same time";

		private readonly INotificationPlatform _platform;
		private readonly NotificationsSettings _settings;

		private List<NotificationScheduler> _registeredSchedulers;

		private DeferredQueue<NotificationRequest> _deferred;

		//Пока соотношение 1 к 1, но пока не придумал кейсов когда нужно больше одного overrider'а
		private Dictionary<Type, Type> _schedulerToOverrider;

		private static readonly TimeSpan SCHEDULE_TOLERANCE = TimeSpan.FromSeconds(60);

		private readonly Dictionary<string, ScheduledStamp> _lastScheduled = new();
		private string _lastHandledIntentId;

		private struct ScheduledStamp
		{
			public DateTime deliveryTime;
			public TimeSpan? repeatInterval;
			public string title;
			public string message;
		}

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

			if (!_settings.disableClearAllOnStart)
			{
				CancelAll();
				RemoveAll();
			}

			_schedulerToOverrider = new Dictionary<Type, Type>();
			var platformInterface = typeof(IPlatformNotification<>).MakeGenericType(PlatformType);
			foreach (var overriderType in ReflectionUtility.GetAllTypes<ISchedulerOverrider>(false))
			{
				if (!platformInterface.IsAssignableFrom(overriderType))
					continue;

				var schedulerType = overriderType.GetInterface(typeof(ISchedulerOverrider<>).Name)!
					.GetGenericArguments()[0];
				_schedulerToOverrider[schedulerType] = overriderType;
			}

			_deferred = new DeferredQueue<NotificationRequest>(ScheduleInternal);

			UnityLifecycle.ApplicationFocusEvent += OnApplicationFocus;
		}

		public void Dispose()
		{
			if (_platform == null)
				return;

			_schedulerToOverrider = null;

			DisposeUtility.DisposeAndSetNull(ref _deferred);

			_platform.NotificationReceived -= OnNotificationReceived;

			UnityLifecycle.ApplicationFocusEvent -= OnApplicationFocus;
		}

		private void OnApplicationFocus()
		{
			// ApplicationFocusEvent мультикастный: бросок отсюда оборвал бы остальных подписчиков,
			// а внутри JNI (GetLastNotificationIntent читает getActiveNotifications)
			try
			{
				var lastIntentNotificationId = GetLastIntentNotificationId();
				if (lastIntentNotificationId == null)
					return;

				// Интент активити переживает сворачивания: без кэша один и тот же интент
				// обрабатывался бы заново на каждом возврате фокуса
				if (lastIntentNotificationId == _lastHandledIntentId)
					return;

				_lastHandledIntentId = lastIntentNotificationId;
				Remove(lastIntentNotificationId);
			}
			catch (Exception e)
			{
				NotificationsDebug.LogError($"Failed to process last notification intent: {e}");
			}
		}

		internal bool Register<T>(T scheduler)
			where T : NotificationScheduler
		{
			// Вызов приходит из конструктора базового класса, где this типизирован как
			// NotificationScheduler, поэтому typeof(T) здесь бесполезен — берем тип с инстанса
			var type = scheduler.GetType();

			if (_settings.disableSchedulers.Contains(type.FullName))
				return false;

			ISchedulerOverrider overrider = null;
			if (_schedulerToOverrider.TryGetValue(type, out var overriderType))
				overrider = overriderType.CreateInstance<ISchedulerOverrider>();
			scheduler.Construct(overrider);

			_registeredSchedulers ??= new();
			_registeredSchedulers.Add(scheduler);

			return true;
		}

		internal bool Unregister<T>(T scheduler) where T : NotificationScheduler
			=> _registeredSchedulers != null && _registeredSchedulers.Remove(scheduler);

		internal void Schedule(ref NotificationRequest request)
		{
			if (!request.remainingTime.HasValue && !request.deliveryTime.HasValue)
				throw NotificationsDebug.Exception(INVALID_ARGS_BY_TIME_LOGS_MESSAGE);

			request.deliveryTime ??= DateTime.Now + request.remainingTime.Value;

			var date = request.deliveryTime!.Value;
			var isUtc = date.Kind == DateTimeKind.Utc;
			var now = isUtc ? DateTime.UtcNow : DateTime.Now;
			if (date <= now)
			{
				NotificationsDebug.LogError(
					$"Trying to schedule notification by id [ {request.id} ] in the past, " +
					$"date: {date.ToShortTimeString()}, {date.ToShortDateString()} (kind:{date.Kind})");
				return;
			}

#if DEV
			NotificationsDebug.ApplySettings(ref request, now);
#endif

			// Планирование на платформе — это JNI, биндер и запись в SharedPreferences за каждый
			// вызов, а шедулеры перепланируют пачками: повторный Schedule с тем же содержимым отсекаем
			if (IsAlreadyScheduled(in request))
				return;

			// Логируем до откладывания, пока в стеке виден инициатор
			NotificationsDebug.Log(SCHEDULED_LOG_MESSAGE_FORMAT.Format(request));

			// Планирование на платформе дорогое, поэтому пачку уведомлений раскладываем по кадрам
			_deferred.Handle(in request);
		}

		private bool IsAlreadyScheduled(in NotificationRequest request)
		{
			if (!_lastScheduled.TryGetValue(request.id, out var stamp))
				return false;

			if (stamp.title != request.title || stamp.message != request.message)
				return false;

			if (stamp.repeatInterval != request.repeatInterval)
				return false;

			var delta = request.deliveryTime!.Value - stamp.deliveryTime;
			return delta.Duration() <= SCHEDULE_TOLERANCE;
		}

		private void ScheduleInternal(in NotificationRequest request)
		{
			if (!_platform.Schedule(in request))
			{
				NotificationsDebug.LogError(FAILED_LOG_MESSAGE_FORMAT.Format(request));
				return;
			}

			_lastScheduled[request.id] = new ScheduledStamp
			{
				deliveryTime = request.deliveryTime!.Value,
				repeatInterval = request.repeatInterval,
				title = request.title,
				message = request.message,
			};
		}

		internal void Cancel(string id)
		{
			_lastScheduled.Remove(id);
			_platform?.Cancel(id);
		}

		internal void CancelAll()
		{
			_lastScheduled.Clear();
			_platform?.CancelAll();
		}

		internal void Remove(string id) => _platform?.Remove(id);

		internal void RemoveAll() => _platform?.RemoveAll();

		internal void OpenApplicationSettings() => _platform?.OpenApplicationSettings();

		internal string GetLastIntentNotificationId() => _platform?.GetLastIntentNotificationId();

		internal IEnumerable<NotificationRequest> EnumerateScheduledNotifications()
			=> _platform?.EnumerateScheduledNotifications() ?? Array.Empty<NotificationRequest>();

		private void OnNotificationReceived(string id, string data)
		{
			// Новая доставка с тем же id — это новый интент: сбрасываем маркер «уже обработано»,
			// иначе повторный тап не уберёт уведомление из шторки
			if (id != null && id == _lastHandledIntentId)
				_lastHandledIntentId = null;

			// TODO: Добавить по надобности Receivers..
		}
	}
}
