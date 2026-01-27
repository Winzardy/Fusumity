#if UNITY_EDITOR
#define FAKE
#endif

using System;
using System.Threading;
using Advertising;
using Advertising.Offline;
using Cysharp.Threading.Tasks;
using Sapientia;
using Sirenix.OdinInspector;
#if FAKE
using Advertising.Fake;
#else
using Content;
using Advertising.UnityLevelPlay;
using ProjectInformation;
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

		public bool useOfflineService;

		private IAdvertisingService _offlineService;
		private IAdvertisingIntegration _integration;

		public override UniTask RunAsync(CancellationToken token = default)
		{
#if FAKE
			_integration = new FakeAdIntegration();
#else
			var settings = ContentManager.Get<UnityLevelPlaySettings>();
			_integration = new UnityLevelPlayAdIntegration(settings, in ProjectInfo.Platform);
#endif
			var management = new AdManagement(_integration);
			AdManager.Set(management);

			if (useOfflineService)
			{
				_offlineService = new OfflineAdvertisingService();
				AdManager.Bind(_offlineService);
			}

			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			// ReSharper disable once SuspiciousTypeConversion.Global
			if (_integration is IDisposable integration)
				integration.Dispose();

			if (_offlineService is IDisposable offlineService)
				offlineService.Dispose();

			AdManager.Clear();
		}

		public override void OnBootCompleted()
		{
			// ReSharper disable once SuspiciousTypeConversion.Global
			if (_offlineService is IInitializable initializable)
				initializable.Initialize();
		}
	}
}
