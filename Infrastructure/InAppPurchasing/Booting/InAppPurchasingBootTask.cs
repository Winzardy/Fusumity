using System;
using System.Threading;
using Content;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using InAppPurchasing;
using InAppPurchasing.Offline;
using InAppPurchasing.Unity;
using Sirenix.OdinInspector;
using Targeting;
using UnityEngine;

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

		[LabelText("Backend")]
		[SerializeReference]
		private IInAppPurchasingServiceFactory _factory = new OfflineInAppPurchasingServiceFactory();

		private IInAppPurchasingService _service;
		private IInAppPurchasingIntegration _integration;

		private UniTaskCompletionSource _storePromotionalCompletionSource;

		public override UniTask RunAsync(CancellationToken cancellationToken = default)
		{
			_storePromotionalCompletionSource = new UniTaskCompletionSource();

			_service = _factory.Create();

			var settings = ContentManager.Get<UnityPurchasingSettings>();
			var unityPurchasingService = new UnityPurchasingIntegration
			(
				_service,
				settings,
				in ProjectDesk.Distribution,
				ProjectDesk.Identifier,
				_storePromotionalCompletionSource
			);

			unityPurchasingService
			   .InitializeAsync(cancellationToken)
			   .ContinueWith(OnInitialized)
			   .Forget();

			_integration = unityPurchasingService;

			var management = new IAPManagement(_integration, _service);
			IAPManager.Initialize(management);

			return UniTask.CompletedTask;

			void OnInitialized(UnityPurchasingInitializationFailureReason failureReason)
			{
				if (failureReason != UnityPurchasingInitializationFailureReason.None)
					IAPDebug.LogError($"Failed to initialize in-app purchasing integration with reason [ {failureReason} ]");
			}
		}

		protected override void OnDispose()
		{
			// ReSharper disable once SuspiciousTypeConversion.Global
			if (_integration is IDisposable disposable)
				disposable.Dispose();

			// ReSharper disable once SuspiciousTypeConversion.Global
			if (_service is IDisposable disposable2)
				disposable2.Dispose();

			_storePromotionalCompletionSource?.TrySetCanceled();

			IAPManager.Terminate();
		}

		public override void OnBootCompleted()
		{
			_storePromotionalCompletionSource.TrySetResult();
			_storePromotionalCompletionSource = null;

			_service.Initialize();
		}
	}
}
