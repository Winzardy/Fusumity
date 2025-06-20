using System;
using System.Collections.Generic;

namespace Notifications
{
	[Serializable]
	public struct NotificationsSettings
	{
		public bool disableClearAllOnStart;

		public List<string> disableSchedulers;
	}
}
