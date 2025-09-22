#if UNITY_EDITOR
#define FAKE
#endif

using System;
using System.Threading;
using Advertising;
using Advertising.Offline;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Sapientia;
using Sirenix.OdinInspector;
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

		private IAdvertisingService _service;
		private IAdvertisingIntegration _integration;

		public override UniTask RunAsync(CancellationToken token = default)
		{
#if FAKE
			_integration = new FakeAdIntegration();
#else
			var settings = ContentManager.Get<UnityLevelPlaySettings>();
			_integration = new UnityLevelPlayAdIntegration(settings, in ProjectDesk.Platform);
#endif
			_service = _factory.Create();
			var management = new AdManagement(_integration, _service);
			AdManager.Initialize(management);

			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			// ReSharper disable once SuspiciousTypeConversion.Global
			if (_integration is IDisposable integration)
				integration.Dispose();

			// ReSharper disable once SuspiciousTypeConversion.Global
			if (_service is IDisposable service)
				service.Dispose();

			AdManager.Terminate();
		}

		public override void OnBootCompleted()
		{
			if (_service is IInitializable service)
				service.Initialize();
		}
	}
}
