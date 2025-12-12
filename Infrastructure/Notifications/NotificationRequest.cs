using System;

namespace Notifications
{
	public interface IPlatformNotificationRequest
	{}

	public struct NotificationRequest
	{
		public readonly string id;

		public NotificationConfig config;

		public string title;
		public string message;

		/// <summary>
		/// Точное время, когда уведомление должно быть доставлено пользователю
		/// </summary>
		public DateTime? deliveryTime;

		/// <summary>
		/// Уведомление будет доставлено через N времени (timespan) с текущего времени
		/// </summary>
		public TimeSpan? remainingTime;

		/// <summary>
		/// Интервал повторения
		/// </summary>
		public TimeSpan? repeatInterval;

		public IPlatformNotificationRequest platform;

		public NotificationRequest(string id, NotificationConfig config) : this()
		{
			this.id = id;
			this.config = config;
		}

		public override string ToString()
		{
			return $"\nId: {id}" +
				$"\nTitle: {title} " +
				$"\nBody: {message} " +
				$"\nDelivery Time: {deliveryTime}";
		}
	}
}
