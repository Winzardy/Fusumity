using System;

namespace Notifications
{
	public interface INotificationPlatform : IDisposable
	{
		/// <summary>
		/// Запланировать уведомление
		/// </summary>
		public void Schedule(in NotificationArgs args);

		/// <summary>
		/// Отменить запланированное (scheduled) уведомление
		/// </summary>
		/// <param name="id"></param>
		public void Cancel(string id);

		/// <summary>
		/// Отменить все запланированные (scheduled) уведомления
		/// </summary>
		/// <param name="id"></param>
		public void CancelAll();

		/// <summary>
		/// Удалить полученное (delivered) уведомление
		/// </summary>
		public void Remove(string id);

		/// <summary>
		/// Удалить все полученные (delivered) уведомление
		/// </summary>
		public void RemoveAll();

		public void OpenApplicationSettings();

		/// <summary>
		/// Id, data
		/// </summary>
		public event Action<string, string> NotificationReceived;

		public string GetLastIntentNotificationId();
	}
}
