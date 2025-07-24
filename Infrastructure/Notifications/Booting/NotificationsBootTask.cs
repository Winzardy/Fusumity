using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Content;
using Fusumity.Reactive;
using Fusumity.Utility;
using Notifications;

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

		public override UniTask RunAsync(CancellationToken token = default)
		{
#if UNITY_EDITOR
			_platform = new EditorNotificationPlatform();
#elif UNITY_ANDROID
			_platform = new Notifications.Android.AndroidNotificationPlatform();
#elif UNITY_IOS
			_platform = new Notifications.iOS.iOSNotificationPlatform();
#endif
			var settings = ContentManager.Get<NotificationsSettings>();
			var management = new NotificationsManagement(settings, _platform);
			NotificationsCenter.Initialize(management);

			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			if (UnityLifecycle.ApplicationQuitting)
				return;

			_platform?.Dispose();
			NotificationsCenter.Terminate();
		}

		public override void OnBootCompleted()
		{
			foreach (var type in ReflectionUtility.GetAllTypes<NotificationScheduler>(false))
			{
				if (!NotificationsCenter.TryRegisterScheduler(type, out var scheduler))
					continue;

				AddDisposable(scheduler);
				scheduler.Initialize();
			}
		}
	}
}
