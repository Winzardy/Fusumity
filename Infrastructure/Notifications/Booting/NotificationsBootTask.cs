using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Content;
using Fusumity.Utility;
using Notifications;
using Sapientia;


#if UNITY_ANDROID
using Notifications.Android;
#elif UNITY_IOS
using Notifications.iOS;
#endif

namespace Booting.Notifications
{
	[TypeRegistryItem(
		"\u2009Notifications", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.BellFill)]
	[Serializable]
	public class NotificationsBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 110;

		private INotificationPlatform _platform;

		public override async UniTask RunAsync(Blackboard _, CancellationToken token = default)
		{
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
			_platform = new EditorNotificationPlatform();
#elif UNITY_ANDROID
			_platform = new AndroidNotificationPlatform();
#elif UNITY_IOS
			var platform = new iOSNotificationPlatform();
			await platform.AuthorizeAsync(token);
			_platform = platform;
#endif
			if (_platform != null)
			{
				var settings = ContentManager.Get<NotificationsSettings>();
				var management = new NotificationsManagement(settings, _platform);
				NotificationsCenter.Set(management);
			}

			await UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			if (_platform == null)
				return;

			_platform.Dispose();
			NotificationsCenter.Clear();
		}
	}
}
