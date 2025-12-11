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
		private UnityPurchasingIntegration _integration;

		private UniTaskCompletionSource _storePromotionalCompletionSource;

		public override UniTask RunAsync(CancellationToken cancellationToken = default)
		{
			_storePromotionalCompletionSource = new UniTaskCompletionSource();

			var settings = ContentManager.Get<UnityPurchasingSettings>();

			_grantCenter = new InAppPurchasingGrantCenter();
			_integration = new UnityPurchasingIntegration
			(
				_grantCenter,
				settings,
				in ProjectInfo.Distribution,
				ProjectInfo.Identifier,
				_storePromotionalCompletionSource
			);

			_integration
				.InitializeAsync(cancellationToken)
				.ContinueWith(OnInitialized)
				.Forget();

			var management = new IAPManagement(_integration
#if IAP_DEBUG
				, _grantCenter
#endif
			);
			IAPManager.Initialize(management);

			if (useOfflineService)
			{
				_offlineService = new OfflineInAppPurchasingService();
				IAPManager.Bind(_offlineService);
			}

			return UniTask.CompletedTask;

			void OnInitialized(UnityPurchasingInitializationFailureReason failureReason)
			{
				if (failureReason != UnityPurchasingInitializationFailureReason.None)
				{
					IAPDebug.LogError($"Failed to initialize in-app purchasing integration with reason [ {failureReason} ]");
					return;
				}

				if (IAPManager.IsRestoreSupported())
					IAPManager.RestoreTransactions();
			}
		}

		protected override void OnDispose()
		{
			// ReSharper disable once SuspiciousTypeConversion.Global
			if (_integration is IDisposable integration)
				integration.Dispose();

			// ReSharper disable once SuspiciousTypeConversion.Global
			if (_offlineService is IDisposable service)
				service.Dispose();

			_storePromotionalCompletionSource?.TrySetCanceled();

			IAPManager.Terminate();
		}

		public override void OnBootCompleted()
		{
			UniTaskUtility.TrySetResultAndSetNull(ref _storePromotionalCompletionSource);

			if (_offlineService is IInitializable service)
				service.Initialize();

			InitializeGrantCenterAsync().Forget();
		}

		private async UniTaskVoid InitializeGrantCenterAsync()
		{
			await UniTask.DelayFrame(2); // На всякий случай ждем 1-2 кадра
			_grantCenter.Initialize();
		}
	}
}
