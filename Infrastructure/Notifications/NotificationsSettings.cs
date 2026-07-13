using System;
using System.Collections.Generic;
using UnityEngine;

namespace Notifications
{
	[Serializable]
	public struct NotificationsSettings
	{
		[Tooltip("Очищаем все уведомления при запуске и пересоздаем. Это решаем вопросы с призрачными уведомлениями")]
		public bool disableClearAllOnStart;

		public List<string> disableSchedulers;
	}
}
