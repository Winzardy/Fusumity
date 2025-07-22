#if UNITY_EDITOR
#define FAKE
#endif

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Advertising;
using Fusumity.Reactive;

#if FAKE
using Advertising.Fake;

#else
using Content;
using Advertising.UnityLevelPlay;
using Targeting;
#endif

namespace Booting.Advertising
{
	[TypeRegistryItem(
		"\u2009Advertising", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.CameraVideo)]
	[Serializable]
	public class AdvertisingBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 60;

		private IAdvertisingIntegration _integration;
		private AdvertisingEventsObserver _observer;

		public override UniTask RunAsync(CancellationToken token = default)
		{
#if FAKE
			_integration = new FakeAdIntegration();
#else
			var settings = ContentManager.Get<UnityLevelPlaySettings>();
			_integration = new UnityLevelPlayAdIntegration(settings, in ProjectDesk.Platform);
#endif
			var management = new AdManagement(_integration);
			AdManager.Initialize(management);

			_observer = new AdvertisingEventsObserver();
			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			// ReSharper disable once SuspiciousTypeConversion.Global
			if (_integration is IDisposable disposable)
				disposable.Dispose();

			_observer.Dispose();

			if (UnityLifecycle.ApplicationQuitting)
				return;

			AdManager.Terminate();
		}
	}
}
