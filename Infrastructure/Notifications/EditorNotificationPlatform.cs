using System;

namespace Notifications
{
	/// <summary>
	/// Обычная заглушка для эдитора, чтобы в редакторе видеть попытку создать нотификацию
	/// </summary>
	public class EditorNotificationPlatform : INotificationPlatform
	{
		public event Action<string, string> NotificationReceived;

		public bool Schedule(in NotificationRequest request)
		{
#if UNITY_EDITOR
			return true;
#else
			return false;
#endif
		}

		public void Cancel(string id)
		{
		}

		public void CancelAll()
		{
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
	}
}
