using System;
using UnityEngine.Scripting;

namespace Notifications
{
	[Preserve]
	public abstract class BasePlatformNotificationSchedulerOverrider<T, TPlatform> : IPlatformNotification<TPlatform>,
		ISchedulerOverrider<T>
		where T : NotificationScheduler
		where TPlatform : INotificationPlatform
	{
		public Type SchedulerType => typeof(T);
		public Type PlatformType => typeof(TPlatform);

		protected T _scheduler;

		public void Initialize(NotificationScheduler scheduler)
		{
			Initialize(scheduler as T);
		}

		private void Initialize(T scheduler)
		{
			_scheduler = scheduler;

			OnInitialize();
		}

		public void Dispose() => OnDispose();

		public abstract void Override(ref NotificationArgs args);

		protected virtual void OnInitialize()
		{
		}

		protected virtual void OnDispose()
		{
		}
	}
}
