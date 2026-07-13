using System;
using System.Collections.Generic;
using System.Linq;

namespace Notifications
{
	/// <summary>
	/// Обычная заглушка для эдитора, чтобы в редакторе видеть попытку создать нотификацию
	/// </summary>
	public class EditorNotificationPlatform : INotificationPlatform
	{
		private readonly Dictionary<string, NotificationRequest> _scheduledNotifications = new();

		public event Action<string, string> NotificationReceived;

		public bool Schedule(in NotificationRequest request)
		{
#if UNITY_EDITOR
			_scheduledNotifications[request.id] = request;
			return true;
#else
			return false;
#endif
		}

		public void Cancel(string id)
		{
			_scheduledNotifications.Remove(id);
		}

		public void CancelAll()
		{
			_scheduledNotifications.Clear();
		}

		public void Remove(string id)
		{
		}

		public void RemoveAll()
		{
		}

		public void OpenApplicationSettings()
		{
		}

		public void Dispose()
		{
		}

		public string GetLastIntentNotificationId() => string.Empty;

		public IReadOnlyList<NotificationRequest> GetScheduledNotifications()
			=> _scheduledNotifications.Values.ToArray();
	}
}
