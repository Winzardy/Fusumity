#if DebugLog
#define IAP_DEBUG
#endif
using System;
using System.Threading;
using Content;
using Cysharp.Threading.Tasks;
using Fusumity.Utility;
using InAppPurchasing;
using InAppPurchasing.Offline;
using InAppPurchasing.Unity;
using ProjectInformation;
using Sapientia;
using Sirenix.OdinInspector;

namespace Booting.InAppPurchasing
{
	[TypeRegistryItem(
		"\u2009InAppPurchasing", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.Cash)]
	[Serializable]
	public class InAppPurchasingBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 50;

		public bool useOfflineService;

		private IInAppPurchasingGrantCenter _grantCenter;
		private IInAppPurchasingService _offlineService;

		public override UniTask RunAsync(Blackboard _, CancellationToken cancellationToken = default)
		{
			_grantCenter = new InAppPurchasingGrantCenter();

			var management = new IAPManagement();
			management.SetGrantCenter(_grantCenter);
			IAPManager.Set(management);

			if (useOfflineService)
			{
				_offlineService = new OfflineInAppPurchasingService();
				IAPManager.Bind(_offlineService);
			}

			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			// ReSharper disable once SuspiciousTypeConversion.Global
			if (_offlineService is IDisposable service)
				service.Dispose();

			IAPManager.Clear();
		}

		public override void OnBootCompleted()
		{
			if (_offlineService is IInitializable service)
				service.Initialize();
		}
	}
}
