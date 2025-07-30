#if UNITY_EDITOR
#define FAKE
#endif

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Advertising;
using Advertising.Offline;
using Fusumity.Reactive;
using UnityEngine;

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

		[LabelText("Backend")]
		[SerializeReference]
		private IAdvertisingServiceFactory _factory = new OfflineAdvertisingServiceFactory();

		private IAdvertisingIntegration _integration;

		public override UniTask RunAsync(CancellationToken token = default)
		{
#if FAKE
			_integration = new FakeAdIntegration();
#else
			var settings = ContentManager.Get<UnityLevelPlaySettings>();
			_integration = new UnityLevelPlayAdIntegration(settings, in ProjectDesk.Platform);
#endif
			var service = _factory?.Create();
			var management = new AdManagement(_integration, service);
			AdManager.Initialize(management);

			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			// ReSharper disable once SuspiciousTypeConversion.Global
			if (_integration is IDisposable disposable)
				disposable.Dispose();

			if (UnityLifecycle.ApplicationQuitting)
				return;

			AdManager.Terminate();
		}
	}
}
