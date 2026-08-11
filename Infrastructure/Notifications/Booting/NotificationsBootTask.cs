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

		protected override async UniTask RunTaskAsync(Blackboard _, IProgress<BootProgressInfo> progress = null, CancellationToken token = default)
		{
			var settings = ContentManager.Get<NotificationsSettings>();

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
			_platform = new EditorNotificationPlatform();
			SetupManagement(settings);
//#elif UNITY_ANDROID
			if (AndroidNotificationPlatform.Initialize())
			{
				// Контент и локализацию читаем на мейн-треде, дальше — регистрация каналов,
				// JNI-обход запланированных уведомлений и рефлексия по сборкам: уводим в пул,
				// чтобы мейн-тред на буте продолжал крутить кадры. Порядок бут-цепочки сохраняется,
				// потому что таск ожидается через await
				var channels = AndroidNotificationPlatform.BuildChannels();

				await UniTask.RunOnThreadPool(() =>
				{
					UnityEngine.AndroidJNI.AttachCurrentThread();
					try
					{
						_platform = new AndroidNotificationPlatform(channels);
						SetupManagement(settings);
					}
					finally
					{
						UnityEngine.AndroidJNI.DetachCurrentThread();
					}
				}, cancellationToken: token);
			}
#elif UNITY_IOS
			var platform = new iOSNotificationPlatform();
			await platform.AuthorizeAsync(token);
			_platform = platform;
			SetupManagement(settings);
#endif
			await UniTask.CompletedTask;
		}

		private void SetupManagement(NotificationsSettings settings)
		{
			if (_platform == null)
				return;

			var management = new NotificationsManagement(settings, _platform);
			NotificationsCenter.Set(management);
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
