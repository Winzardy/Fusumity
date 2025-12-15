using System;

namespace Notifications
{
	/// <summary>
	/// Перезаписывает или дополняет нотификацию, например для отдельных платформ
	/// </summary>
	public interface ISchedulerOverrider<T> : ISchedulerOverrider
		where T : NotificationScheduler
	{
		public Type SchedulerType { get; }
	}

	public interface ISchedulerOverrider : IDisposable
	{
		public void Initialize(NotificationScheduler scheduler);

		public void Override(ref NotificationRequest request);
	}
}
