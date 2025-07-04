﻿using System;

namespace Notifications
{
	public interface IPlatformNotificationArgs
	{}

	public struct NotificationArgs
	{
		public readonly string id;

		public NotificationEntry entry;

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

		public IPlatformNotificationArgs platform;

		public NotificationArgs(string id, NotificationEntry entry) : this()
		{
			this.id = id;
			this.entry = entry;
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
