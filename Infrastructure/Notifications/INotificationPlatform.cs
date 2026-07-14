using System;
using System.Collections.Generic;

namespace Notifications
{
	public interface INotificationPlatform : IDisposable
	{
		/// <summary>
		/// Запланировать уведомление
		/// </summary>
		bool Schedule(in NotificationRequest request);

		/// <summary>
		/// Отменить запланированное (scheduled) уведомление
		/// </summary>
		/// <param name="id"></param>
		void Cancel(string id);

		/// <summary>
		/// Отменить все запланированные (scheduled) уведомления
		/// </summary>
		/// <param name="id"></param>
		void CancelAll();

		/// <summary>
		/// Удалить полученное (delivered) уведомление
		/// </summary>
		void Remove(string id);

		/// <summary>
		/// Удалить все полученные (delivered) уведомление
		/// </summary>
		void RemoveAll();

		void OpenApplicationSettings();

		/// <summary>
		/// Id, data
		/// </summary>
		event Action<string, string> NotificationReceived;

		string GetLastIntentNotificationId();

		IEnumerable<NotificationRequest> EnumerateScheduledNotifications();
	}
}
