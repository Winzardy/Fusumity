using System;
using System.Threading;
using Content;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using InAppPurchasing;
using InAppPurchasing.Unity;
using Sirenix.OdinInspector;
using Targeting;

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

		private IInAppPurchasingService _service;
		private UniTaskCompletionSource _storePromotionalCompletionSource;

		private InAppPurchasingEventsObserver _observer;

		public override UniTask RunAsync(CancellationToken cancellationToken = default)
		{
			_storePromotionalCompletionSource = new UniTaskCompletionSource();

			var settings = ContentManager.Get<UnityPurchasingSettings>();
			var unityPurchasingService = new UnityPurchasingService
			(
				settings,
				in ProjectDesk.Distribution,
				ProjectDesk.Identifier,
				_storePromotionalCompletionSource
			);

			unityPurchasingService
			   .InitializeAsync(cancellationToken)
			   .ContinueWith(OnInitialized)
			   .Forget();

			_service = unityPurchasingService;

			var management = new IAPManagement(_service);
			IAPManager.Initialize(management);

			_observer = new InAppPurchasingEventsObserver();

			return UniTask.CompletedTask;

			void OnInitialized(UnityPurchasingInitializationFailureReason failureReason)
			{
				if (failureReason != UnityPurchasingInitializationFailureReason.None)
					IAPDebug.LogError($"Failed to initialize in-app purchasing service with reason [ {failureReason} ]");
			}
		}

		protected override void OnDispose()
		{
			_observer?.Dispose();

			// ReSharper disable once SuspiciousTypeConversion.Global
			if (_service is IDisposable disposable)
				disposable.Dispose();

			_storePromotionalCompletionSource?.TrySetCanceled();

			if (UnityLifecycle.ApplicationQuitting)
				return;

			IAPManager.Terminate();
		}

		public override void OnBootCompleted()
		{
			_storePromotionalCompletionSource.TrySetResult();
			_storePromotionalCompletionSource = null;
		}
	}
}
