#if (UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_TVOS) && !UNITY_EDITOR
#define APP_STORE
#endif

#if (UNITY_ANDROID) && !UNITY_EDITOR
#define APP_GOOGLE_PLAY
#endif

using System;
using System.Collections.Generic;
using System.Threading;
using Content;
using Cysharp.Threading.Tasks;
using Fusumity.Utility;
using Fusumity.Utility.UserLocator;
using JetBrains.Annotations;
using ProjectInformation;
using Sapientia;
using Sapientia.Collections;
using Sapientia.Extensions;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

#if !XSOLLA_SDK_DISABLED
using Xsolla.SDK.Common;
using Xsolla.SDK.Store;
using Xsolla.SDK.UnityPurchasing;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InAppPurchasing.Unity
{
	using UnityProduct = Product;

	[Serializable]
	public struct UnityPurchasingSettings
	{
		/// <summary>
		/// Включает или выключает проверку чеков для всех площадок во время обработки покупок (доступно только в App Store и Google Play!)
		/// </summary>
		public bool disableValidationRecipe;

		#region Apple

		/// <summary>
		/// Включает или выключает проверку чеков Apple во время обработки покупок
		/// </summary>
		/// <remarks>
		/// Ни на что не влияет начиная с Unity IAP 5: локальной проверки чеков Apple больше нет,
		/// почему так — в <see cref="UnityPurchasingIntegration.TryInitializeLocalValidator"/>
		/// </remarks>
		public bool appleDisableValidationRecipe;

		/// <summary>
		/// Продолжать ли покупку при Apple App Store Promotional (когда нажали в магазине на продукт)
		/// </summary>
		public bool applePromotionalContinuePurchase;

		/// <summary>
		/// Задержка перед продолжением...
		/// </summary>
		public int applePromotionalContinueDelayMs;

		#endregion

		#region Google Play

		/// <summary>
		/// Включает или выключает проверку чеков Google Play во время обработки покупок
		/// </summary>
		public bool googlePlayDisableValidationRecipe;

		#endregion

		#region Xsolla

		/// <summary>
		/// Включает или выключает проверку чеков Xsolla
		/// </summary>
		public bool xsollaDisableValidationRecipe;

		#endregion

		public Dictionary<DistributionEntry, BillingScheme> storeToScheme;
	}

	[Serializable]
	public struct BillingScheme
	{
		public Toggle<IAPBillingEntry> overrideBilling;
		public Dictionary<CountryEntry, IAPBillingEntry> countryToBilling;
	}

	public partial class UnityPurchasingIntegration : IInAppPurchasingIntegration, IDisposable
	{
		public string Name => "UnityPurchasing";

		private readonly string _appIdentifier;

		private readonly UnityPurchasingSettings _settings;
		private readonly DistributionEntry _distributionPlatform;
		private IAPBillingEntry _billing;

		private StoreController _storeController;

		[CanBeNull]
		private IAppleStoreExtendedService _appleService;

		[CanBeNull]
		private IAppleStoreExtendedPurchaseService _applePurchaseService;

#if !XSOLLA_SDK_DISABLED
		[CanBeNull]
		private IXsollaPurchasingStoreExtension _xsollaExtension;
#endif

		/// <summary>
		/// Валидатор чеков, начиная с Unity IAP 5 работает только для Google Play
		/// </summary>
		[CanBeNull]
		private CrossPlatformValidator _localValidator;

		private UniTaskCompletionSource<UnityPurchasingInitializationFailureReason> _productsCompletionSource;

		private bool _initialized;

		#region Apple Configuration

		private bool _appleCanMakePayments;

		#endregion

		/// <summary>
		/// Список продуктов, которые в данный момент обрабатываются (Billing Product ID)
		/// </summary>
		private HashSet<string> _processing;

		/// <summary>
		/// Продукты, покупка которых отложена (Billing Product ID)
		/// </summary>
		private HashSet<string> _deferred;

		/// <summary>
		/// Магазинный ID продукта - Запись продукта. Важно понимать что ID для разных платформ может отличаться!
		/// </summary>
		private BidirectionalMap<string, IAPProductEntry> _billingProductIdToEntry;

		/// <summary>
		/// Магазинный ID продукта - последний известный заказ, из него берётся информация о подписке
		/// </summary>
		private Dictionary<string, Order> _billingProductIdToOrder;

		/// <summary>
		/// Продукты, которыми игрок владеет, заменяет <c>Product.hasReceipt</c> из Unity IAP 4.
		/// Наполняется из заказов: <c>FetchPurchases</c> отдаёт всё, что игрок уже купил
		/// </summary>
		private HashSet<string> _purchased;

		/// <summary>
		/// Нужен чтобы контролировать из вне когда "продолжить" покупки из промо (пока только для app store)
		/// </summary>
		private readonly UniTaskCompletionSource _storePromotionalCompletionSource;

		private readonly IInAppPurchasingGrantCenter _grantCenter;

		public ref readonly IAPBillingEntry Billing { get => ref _billing; }

		public event PurchaseCompleted PurchaseCompleted;
		public event PurchaseFailed PurchaseFailed;
		public event PurchaseRequested PurchaseRequested;
		public event PurchaseCanceled PurchaseCanceled;
		public event PurchaseDeferred PurchaseDeferred;
		public event PromotionalPurchaseIntercepted PromotionalPurchaseIntercepted;

		public UnityPurchasingIntegration()
		{
		}

		public UnityPurchasingIntegration(
			IInAppPurchasingGrantCenter grantCenter,
			in UnityPurchasingSettings settings,
			in DistributionEntry distributionPlatform,
			string appIdentifier,
			UniTaskCompletionSource storePromotionalCompletionSource = null)
		{
			_grantCenter                      = grantCenter;
			_settings                         = settings;
			_distributionPlatform             = distributionPlatform;
			_appIdentifier                    = appIdentifier;
			_storePromotionalCompletionSource = storePromotionalCompletionSource;
		}

		public void Dispose()
		{
			if (_storeController == null)
				return;

			_storeController.OnStoreConnected       -= OnStoreConnected;
			_storeController.OnStoreDisconnected    -= OnStoreDisconnected;
			_storeController.OnProductsFetched      -= OnProductsFetched;
			_storeController.OnProductsFetchFailed  -= OnProductsFetchFailed;
			_storeController.OnPurchasesFetched     -= OnPurchasesFetched;
			_storeController.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
			_storeController.OnPurchasePending      -= OnPurchasePending;
			_storeController.OnPurchaseConfirmed    -= OnPurchaseConfirmed;
			_storeController.OnPurchaseFailed       -= OnPurchaseFailed;
			_storeController.OnPurchaseDeferred     -= OnPurchaseDeferred;

			if (_applePurchaseService != null)
				_applePurchaseService.OnPromotionalPurchaseIntercepted -= OnApplePromotionalPurchaseInterceptor;

			_initialized     = false;
			_storeController = null;
		}

		public async UniTask<UnityPurchasingInitializationFailureReason> InitializeAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				_initialized = false;

				var success = await UnityServices.UnityServiceInitializationAsync(cancellationToken);
				cancellationToken.ThrowIfCancellationRequested();

				if (!success)
					return UnityPurchasingInitializationFailureReason.UnityServices;

				var failureReason = await SetupTargetBillingAsync(cancellationToken);

				if (failureReason != UnityPurchasingInitializationFailureReason.None)
					return failureReason;

#if !UNITY_EDITOR
				if (_billing == IAPBillingType.UNDEFINED)
					return UnityPurchasingInitializationFailureReason.UnknownBilling;
#endif

				if (_storeController == null)
					CreateStoreController();

				_billingProductIdToEntry = new BidirectionalMap<string, IAPProductEntry>(4);

				var products = new List<ProductDefinition>(4);
				AddProducts<IAPConsumableProductEntry>(products);
				AddProducts<IAPNonConsumableProductEntry>(products);
				AddProducts<IAPSubscriptionProductEntry>(products);

				var productsStr = products.GetCompositeString(definition => definition.storeSpecificId, true);
				IAPDebug.Log($"UnityPurchasing connecting, billing: {_billing}, products:{productsStr}");

				await _storeController.Connect();
				cancellationToken.ThrowIfCancellationRequested();

				if (_storeController.GetConnectionState() != ConnectionState.Connected)
					return UnityPurchasingInitializationFailureReason.PurchasingUnavailable;

				SetupExtendedServices();

				_productsCompletionSource = new UniTaskCompletionSource<UnityPurchasingInitializationFailureReason>();
				_storeController.FetchProducts(products);
				failureReason = await _productsCompletionSource.Task.AttachExternalCancellation(cancellationToken);
				cancellationToken.ThrowIfCancellationRequested();

				if (failureReason != UnityPurchasingInitializationFailureReason.None)
					return failureReason;

				TryInitializeLocalValidator();

				_initialized = true;

				// Незавершённые и восстановленные покупки приезжают обратно через OnPurchasePending
				_storeController.FetchPurchases();

				return UnityPurchasingInitializationFailureReason.None;
			}
			catch (OperationCanceledException)
			{
				return UnityPurchasingInitializationFailureReason.Canceled;
			}
			catch (Exception ex)
			{
				IAPDebug.LogException(ex);
				return UnityPurchasingInitializationFailureReason.Exception;
			}
		}

		private void CreateStoreController()
		{
			string storeName = null;

#if !XSOLLA_SDK_DISABLED
			if (_billing == IAPBillingType.XSOLLA)
			{
				var settings = XsollaClientSettingsAsset.Instance().settings;

#if UNITY_IOS
				settings = XsollaClientSettings.Builder.Update(settings)
					.SetWebViewType(XsollaClientSettings.WebViewType.External) // or .Auto for EU
					.Build();
#endif

				var configuration = XsollaStoreClientConfiguration.Builder.Create()
					.SetSettings(settings)
					.SetUserId(ProjectInfo.UserId)
#if DEV
					.SetSandbox(true)
					.SetLogLevel(XsollaLogLevel.Debug)
#endif
					.Build();
				var module = XsollaPurchasingModule.Builder.Create()
					.SetConfiguration(configuration)
					.Build();

				UnityIAPServices.AddNewCustomStore(module);

				_xsollaExtension = module.Extension;
				storeName        = XsollaPurchasingModule.StoreName;
			}
#endif

			_storeController = UnityIAPServices.StoreController(storeName);

			// Магазин может отдать незавершённую покупку сразу после подключения, состояние нужно раньше подписки
			_processing              = new HashSet<string>(2);
			_deferred                = new HashSet<string>(2);
			_billingProductIdToOrder = new Dictionary<string, Order>(2);
			_purchased               = new HashSet<string>(2);

			// Подписка до Connect обязательна: незавершённые покупки прилетают сразу после подключения
			_storeController.OnStoreConnected       += OnStoreConnected;
			_storeController.OnStoreDisconnected    += OnStoreDisconnected;
			_storeController.OnProductsFetched      += OnProductsFetched;
			_storeController.OnProductsFetchFailed  += OnProductsFetchFailed;
			_storeController.OnPurchasesFetched     += OnPurchasesFetched;
			_storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
			_storeController.OnPurchasePending      += OnPurchasePending;
			_storeController.OnPurchaseConfirmed    += OnPurchaseConfirmed;
			_storeController.OnPurchaseFailed       += OnPurchaseFailed;
			_storeController.OnPurchaseDeferred     += OnPurchaseDeferred;
		}

		/// <summary>
		/// Расширения магазинов доступны только после <c>Connect</c>
		/// </summary>
		private void SetupExtendedServices()
		{
			switch (_billing)
			{
				case IAPBillingType.APP_STORE:
					_appleService         = _storeController.AppleStoreExtendedService;
					_applePurchaseService = _storeController.AppleStoreExtendedPurchaseService;

					_appleCanMakePayments = _appleService?.canMakePayments ?? false;

					if (_applePurchaseService != null)
					{
						_applePurchaseService.OnPromotionalPurchaseIntercepted -= OnApplePromotionalPurchaseInterceptor;
						_applePurchaseService.OnPromotionalPurchaseIntercepted += OnApplePromotionalPurchaseInterceptor;
					}

					break;
			}
		}

		private async UniTask<UnityPurchasingInitializationFailureReason> SetupTargetBillingAsync(CancellationToken cancellationToken)
		{
			var autoSetDefaultBilling = true;

			if (_settings.storeToScheme.TryGetValue(_distributionPlatform, out var scheme))
			{
				try
				{
					string country = await UserLocator.GetCountryAsync(cancellationToken);

					if (country != UserLocator.UNDEFINED && scheme.countryToBilling.TryGetValue(country, out var billing))
					{
						_billing = billing;
						autoSetDefaultBilling = false;
					}
				}
				catch (Exception e)
				{
					IAPDebug.LogException(e);
				}

				cancellationToken.ThrowIfCancellationRequested();

				if (autoSetDefaultBilling && scheme.overrideBilling)
				{
					_billing = scheme.overrideBilling;
				}
			}

			if (autoSetDefaultBilling)
				_billing = GetDefaultBilling(_distributionPlatform);

			return UnityPurchasingInitializationFailureReason.None;
		}

		public bool IsInitialized() => _storeController != null && _initialized;

		public bool TryGetStatus(IAPProductEntry product, out ProductStatus status)
		{
			status = ProductStatus.Undefined;

			if (!IsInitialized())
			{
				status = ProductStatus.NotInitialized;
				return true;
			}

			var billingProductId = product.GetBillingId(in _billing);

			if (_processing.Contains(billingProductId))
			{
				status = ProductStatus.AlreadyProcessing;
				return true;
			}

			if (_distributionPlatform == DistributionType.APP_STORE)
			{
				if (!_appleCanMakePayments)
				{
					status = ProductStatus.NotAvailable;
					return true;
				}
			}

			if (!_billingProductIdToEntry.TryGetValue(billingProductId, out var entry))
			{
				status = ProductStatus.Unknown;
				return true;
			}

			if (!TryGetUnityProduct(billingProductId, out var unityProduct))
			{
				status = ProductStatus.Unknown;
				return true;
			}

			if (!unityProduct.availableToPurchase)
			{
				status = ProductStatus.NotAvailable;
				return true;
			}

			if (IsPurchased(billingProductId, entry))
			{
				status = ProductStatus.Purchased;
				return true;
			}

			if (_deferred.Contains(billingProductId))
			{
				status = ProductStatus.Deferred;
				return true;
			}

			return false;
		}

		public bool SimulateAskToBuy => _applePurchaseService?.simulateAskToBuy ?? false;

		public void SetSimulateAskToBuy(bool value)
		{
			if (_applePurchaseService != null)
				_applePurchaseService.simulateAskToBuy = value;
		}

		#region Restore

		public bool IsRestoreTransactionsSupported => _applePurchaseService != null;

		public void RestoreTransactions()
		{
			if (_applePurchaseService == null)
				return;

			_storeController.RestoreTransactions(OnRestoredTransactions);

			void OnRestoredTransactions(bool success, string error)
			{
				if (success)
					IAPDebug.Log("Restored transactions");
				else
					IAPDebug.LogError("Failed to restore transactions: " + error);
			}
		}

		#endregion

		#region Consumable

		public bool CanPurchaseConsumable(IAPProductEntry product, out IAPPurchaseError? error)
			=> CanPurchase(product, out error);

		/// <returns>Возвращает успешность запроса на покупку, а не статус покупки</returns>
		public bool RequestPurchaseConsumable(IAPProductEntry entry)
		{
			if (!CanPurchaseConsumable(entry, out _))
				return false;

			return RequestPurchase(entry);
		}

		#endregion

		#region Non-Consumable

		public bool CanPurchaseNonConsumable(IAPProductEntry entry, out IAPPurchaseError? error)
		{
			if (!TryGetUnityProduct(entry, out UnityProduct unityProduct))
			{
				error = IAPPurchaseErrorCode.ProductNotFoundInService;
				return false;
			}

			if (IsPurchased(entry.GetBillingId(in _billing), entry))
			{
				error = IAPPurchaseErrorCode.Purchased;
				return false;
			}

			return CanPurchase(entry, out error);
		}

		/// <returns>Возвращает успешность запроса на покупку, а не статус покупки</returns>
		public bool RequestPurchaseNonConsumable(IAPProductEntry entry)
		{
			if (!CanPurchaseNonConsumable(entry, out _))
				return false;

			return RequestPurchase(entry);
		}

		#endregion

		private bool TryGetUnityProduct(IAPProductEntry product, out UnityProduct unityProduct)
			=> TryGetUnityProduct(product.GetBillingId(in _billing), out unityProduct);

		private bool TryGetUnityProduct(string billingProductId, out UnityProduct product)
		{
			product = _storeController?.GetProductById(billingProductId);
			return product != null;
		}

		private bool CanPurchase(IAPProductEntry product, out IAPPurchaseError? error)
		{
			error = null;

			if (!IsInitialized())
			{
				error = IAPPurchaseErrorCode.NotInitialized;
				return false;
			}

			var billingProductId = product.GetBillingId(in _billing);
			if (_processing.Contains(billingProductId))
			{
				error = IAPPurchaseErrorCode.InProgress;
				return false;
			}

			return true;
		}

		private bool RequestPurchase(IAPProductEntry product)
		{
			var billingProductId = product.GetBillingId(in _billing);

			if (!TryGetUnityProduct(billingProductId, out var unityProduct))
				return false;

			if (!_processing.Add(billingProductId))
				return false;

			var entry = _billingProductIdToEntry[billingProductId];
			PurchaseRequested?.Invoke(entry);

			_storeController.PurchaseProduct(unityProduct);
			return true;
		}

		#region Store Listener

		private void OnStoreConnected()
		{
			IAPDebug.Log($"UnityPurchasing successfully connected, billing: {_billing}");
		}

		private void OnStoreDisconnected(StoreConnectionFailureDescription failureDescription)
		{
			IAPDebug.LogError($"Failed to connect: {failureDescription.Message}");
		}

		private void OnProductsFetched(List<UnityProduct> products)
		{
			var productsStr = products.GetCompositeString(product => product.definition.storeSpecificId, true);
			IAPDebug.Log($"UnityPurchasing fetched products, billing: {_billing}, products:{productsStr}");

			_productsCompletionSource?.TrySetResult(UnityPurchasingInitializationFailureReason.None);
		}

		private void OnProductsFetchFailed(ProductFetchFailed failure)
		{
			var productsStr = failure.FailedFetchProducts.GetCompositeString(definition => definition.storeSpecificId, true);
			IAPDebug.LogError($"Failed to fetch products: {failure.FailureReason}, products:{productsStr}");

			_productsCompletionSource?.TrySetResult(UnityPurchasingInitializationFailureReason.NoProductsAvailable);
		}

		private void OnPurchasesFetched(Orders orders)
		{
			IAPDebug.Log($"UnityPurchasing fetched purchases, confirmed: {orders.ConfirmedOrders.Count}, " +
				$"pending: {orders.PendingOrders.Count}, deferred: {orders.DeferredOrders.Count}");

			// Pending приезжают отдельно через OnPurchasePending, здесь только уже завершённые
			foreach (var order in orders.ConfirmedOrders)
				RegisterOrder(order);

			foreach (var order in orders.DeferredOrders)
			{
				if (TryGetBillingProductId(order, out var billingProductId))
					_deferred.Add(billingProductId);
			}
		}

		private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failureDescription)
		{
			IAPDebug.LogError($"Failed to fetch purchases: {failureDescription.FailureReason} {failureDescription.Message}");
		}

		private void OnPurchasePending(PendingOrder order)
		{
			if (!TryGetBillingProductId(order, out var billingProductId))
			{
				IAPDebug.LogError("Failed to purchase: order without a product");
				_storeController.ConfirmPurchase(order);
				return;
			}

			RegisterOrder(billingProductId, order);
			_deferred.Remove(billingProductId);

			if (!_billingProductIdToEntry.TryGetValue(billingProductId, out var entry))
			{
				_processing.Remove(billingProductId);
				IAPDebug.LogError($"Failed to purchase: Not found product by product id [ {billingProductId} ]");
				return;
			}

			var transactionId = order.Info.TransactionID;
			if (transactionId.IsNullOrEmpty())
			{
				_processing.Remove(billingProductId);
				IAPDebug.LogError($"[{entry.Type}] Failed to purchase: empty transaction id " +
					$"for product id [ {billingProductId} ] (billing: {_billing})");
				_storeController.ConfirmPurchase(order);
				return;
			}

			if (IAPManager.ContainsReceipt(transactionId))
			{
				_processing.Remove(billingProductId);
				IAPDebug.LogError($"[{entry.Type}] Failed to purchase: Transaction by id [ {transactionId} ] has already been completed " +
					$"for product id [ {billingProductId} ] (billing: {_billing})");
				_storeController.ConfirmPurchase(order);
				return;
			}

			if (!LocalValidateReceipt(entry, billingProductId, order))
			{
				OnPurchaseFailedInternal(entry, "Invalid receipt", order);
				_storeController.ConfirmPurchase(order);
				return;
			}

			try
			{
				var receipt = new PurchaseReceipt
				{
					productType = entry.Type,
					productId   = entry.Id,

					billing = _billing,

					transactionId = transactionId,
					receipt       = order.Info.Receipt
				};

				if (_processing.Remove(billingProductId))
				{
					if (!PendingValidateReceipt(order, CompleteProcessingAfterValidation))
						CompleteProcessingAfterValidation(true, null);

					return;

					void CompleteProcessingAfterValidation(bool success, string error)
					{
						if (error.IsNullOrEmpty() && success)
						{
							Complete(receipt, true);

							return;
						}

						OnPurchaseFailedInternal(entry, error, order);
					}
				}

				if (!PendingValidateReceipt(order, CompleteGrantAfterValidation))
					CompleteGrantAfterValidation(true, null);

				return;

				void CompleteGrantAfterValidation(bool success, string error)
				{
					if (error.IsNullOrEmpty() && success)
					{
						receipt.isRestored = true;
						_grantCenter.Grant(in receipt, OnComplete);
						return;
					}

					OnPurchaseFailedInternal(entry, error, order);
				}
			}
			catch (Exception e)
			{
				OnPurchaseFailedInternal(entry, e.Message, order);
				return;
			}

			void OnComplete(in PurchaseReceipt receipt) =>
				Complete(receipt, false);

			void Complete(in PurchaseReceipt receipt, bool live)
			{
				_storeController.ConfirmPurchase(order);
				if (live)
					IAPManager.RegisterReceipt(in receipt);
				PurchaseCompleted?.Invoke(in receipt, live, order);
			}
		}

		private void OnPurchaseConfirmed(Order order)
		{
			if (!TryGetBillingProductId(order, out var billingProductId))
				return;

			switch (order)
			{
				case ConfirmedOrder:
					RegisterOrder(billingProductId, order);
					break;

				case FailedOrder failedOrder:
					IAPDebug.LogError($"Failed to confirm purchase by product id [ {billingProductId} ]: " +
						$"{failedOrder.FailureReason} {failedOrder.Details}");
					break;
			}
		}

		private void OnPurchaseFailed(FailedOrder failedOrder)
		{
			if (!TryGetBillingProductId(failedOrder, out var billingProductId))
			{
				IAPDebug.LogError($"Failed to purchase product (not found!): {failedOrder.FailureReason} {failedOrder.Details}");
				return;
			}

			_deferred.Remove(billingProductId);

			if (!_billingProductIdToEntry.TryGetValue(billingProductId, out var entry))
			{
				IAPDebug.LogError(
					$"Failed to purchase product (not found!) by store product id [ {billingProductId} ]: {failedOrder.Details}");
				return;
			}

			switch (failedOrder.FailureReason)
			{
				case PurchaseFailureReason.UserCancelled:
					OnPurchaseCanceledInternal(entry, failedOrder);
					break;

				default:
					OnPurchaseFailedInternal(entry, $"{failedOrder.FailureReason} {failedOrder.Details}", failedOrder);
					break;
			}
		}

		private void OnPurchaseDeferred(DeferredOrder order)
		{
			if (!TryGetBillingProductId(order, out var billingProductId))
			{
				IAPDebug.LogError("Failed to deferred product (not found!): order without a product");
				return;
			}

			_processing.Remove(billingProductId);
			_deferred.Add(billingProductId);

			if (_billingProductIdToEntry.TryGetValue(billingProductId, out var entry))
				PurchaseDeferred?.Invoke(entry, order);
			else
				IAPDebug.LogError($"Failed to deferred product (not found!) by store product id [ {billingProductId} ]");
		}

		private bool PendingValidateReceipt(Order order, Action<bool, string> onComplete)
		{
#if !XSOLLA_SDK_DISABLED
			if (_billing == IAPBillingType.XSOLLA && !_settings.xsollaDisableValidationRecipe)
			{
				var xsollaValidator = _xsollaExtension.GetValidator();
				xsollaValidator.Validate(order.Info.Receipt, onComplete);
				return true;
			}
#endif

			return false;
		}

		private bool LocalValidateReceipt(IAPProductEntry entry, string billingProductId, Order order)
		{
			const string PREFIX = "[ Validation ]";
			const int TOLERANCE_MINUTES = 5;

			if (_localValidator == null)
				return true;

			try
			{
				var receipts = _localValidator.Validate(order.Info.Receipt);

				if (receipts.IsNullOrEmpty())
				{
					IAPDebug.LogError($"{PREFIX} Receipt is null or empty for product id [ {billingProductId} ]");
					return false;
				}

				foreach (var receipt in receipts)
				{
					if (receipt.productID != billingProductId)
					{
						IAPDebug.LogError(
							$"{PREFIX} Product ID mismatch: receipt = {receipt.productID}, expected = {billingProductId}");
						return false;
					}

					if (entry.Type != IAPProductType.Consumable)
					{
						if (receipt.purchaseDate == DateTime.MinValue)
						{
							IAPDebug.LogError(
								$"{PREFIX} Missing purchase date for non-consumable product [ {billingProductId} ]");
							return false;
						}

						var purchaseDate = receipt.purchaseDate;
						var nowDate = IAPManager.DateTime;
						if (purchaseDate > nowDate.AddMinutes(TOLERANCE_MINUTES))
						{
							IAPDebug.LogWarning($"{PREFIX} Future-dated purchase (possibly deferred): {purchaseDate} > {nowDate}");
						}
					}
				}

				return true;
			}
			catch (Exception e)
			{
				IAPDebug.LogError(
					$"{PREFIX} Failed to validate: Exception for product by store product id [ {billingProductId} ] " +
					$"(type: {entry.Type}): {e.Message}");
				return false;
			}
		}

		private void OnApplePromotionalPurchaseInterceptor(UnityProduct product)
		{
			var billingProductId = product.definition.storeSpecificId;

			if (!_billingProductIdToEntry.TryGetValue(billingProductId, out var entry))
			{
				IAPDebug.LogError($"[{product.definition.type}] Failed to apple promotional purchase: Not found product by id [ " +
					billingProductId + " ]");
				return;
			}

			if (_settings.applePromotionalContinuePurchase)
				OnApplePromotionalPurchaseInterceptorAsync().Forget();

			PromotionalPurchaseIntercepted?.Invoke(entry, product);
		}

		private async UniTaskVoid OnApplePromotionalPurchaseInterceptorAsync()
		{
			if (_storePromotionalCompletionSource != null)
				await _storePromotionalCompletionSource.Task;

			await UniTask.Delay(_settings.applePromotionalContinueDelayMs);
			_applePurchaseService!.ContinuePromotionalPurchases();
		}

		#endregion

		#region Orders

		private void RegisterOrder(Order order)
		{
			if (TryGetBillingProductId(order, out var billingProductId))
				RegisterOrder(billingProductId, order);
		}

		private void RegisterOrder(string billingProductId, Order order)
		{
			_billingProductIdToOrder[billingProductId] = order;

			if (_billingProductIdToEntry.TryGetValue(billingProductId, out var entry) && entry.Type != IAPProductType.Consumable)
				_purchased.Add(billingProductId);
		}

		private bool TryGetOrder(string billingProductId, out Order order)
			=> _billingProductIdToOrder.TryGetValue(billingProductId, out order);

		private static bool TryGetBillingProductId(Order order, out string billingProductId)
		{
			billingProductId = null;

			var items = order?.CartOrdered?.Items();
			if (items == null || items.Count == 0)
				return false;

			var item = items[0];
			billingProductId = item.Product.catalogListings.TryGetValue(item.CatalogListingId, out var listing) && listing.definition != null
				? listing.definition.storeSpecificId
				: item.Product.definition?.storeSpecificId;

			return !billingProductId.IsNullOrEmpty();
		}

		/// <summary>
		/// Заменяет <c>Product.hasReceipt</c> из Unity IAP 4: владение определяется по заказам,
		/// которые магазин отдаёт на <c>FetchPurchases</c>
		/// </summary>
		private bool IsPurchased(string billingProductId, IAPProductEntry entry)
		{
			if (entry.Type == IAPProductType.Consumable)
				return false;

			if (entry.Type == IAPProductType.Subscription)
				return TryGetUnitySubscriptionInfo(billingProductId, out var subscriptionInfo) && subscriptionInfo.IsActive();

			return _purchased.Contains(billingProductId);
		}

		#endregion

		private void OnPurchaseFailedInternal(IAPProductEntry entry, string error, object rawData = null)
		{
			var billingProductId = _billingProductIdToEntry[entry];
			_processing.Remove(billingProductId);
			IAPDebug.LogError($"[{entry.Type}] Failed to purchase product by product id [ {billingProductId} ]: {error}");
			PurchaseFailed?.Invoke(entry, error, rawData);
		}

		private void OnPurchaseCanceledInternal(IAPProductEntry entry, object rawData = null)
		{
			var billingProductId = _billingProductIdToEntry[entry];
			_processing.Remove(billingProductId);
			IAPDebug.Log($"[{entry.Type}] Cancel to purchase product by product id [ {billingProductId} ]");
			PurchaseCanceled?.Invoke(entry, rawData);
		}

		private void AddProducts<TProduct>(List<ProductDefinition> products)
			where TProduct : IAPProductEntry
		{
			foreach (var entry in ContentManager.GetAllEntries<TProduct>())
			{
				ref readonly var product = ref entry.Value;
				var id = product.GetBillingId(in _billing);
				_billingProductIdToEntry[id] = product;
				products.Add(new ProductDefinition(id, product.ToUnityProductType()));
			}
		}

		/// <summary>
		/// Собирает локальный валидатор чеков, начиная с Unity IAP 5 он имеет смысл только для Google Play
		/// </summary>
		/// <remarks>
		/// App Store остался без локальной проверки, и это вынужденно, а не по желанию.
		/// На iOS 15+ (у нас это минимальная версия) Unity IAP работает через StoreKit 2, а
		/// <see cref="CrossPlatformValidator"/> в этом режиме для App Store не проверяет ничего и
		/// возвращает пустой массив — старый путь через <c>AppleValidator</c> и <c>AppleTangle</c>
		/// живёт только под StoreKit 1 (<c>StoreKitSelector</c>: StoreKit 1 берётся при major версии iOS меньше 15).
		/// Пустой результат <see cref="LocalValidateReceipt"/> считает невалидным чеком, так что оставь мы
		/// Apple подключённым к валидатору — падала бы каждая покупка на iOS.
		/// <para>
		/// Чем это компенсируется: StoreKit 2 отдаёт приложению только проверенные транзакции, криптографию
		/// (PKCS7 и корневой сертификат Apple), которую раньше крутил <c>AppleValidator</c>, теперь делает сама ОС.
		/// Для серверной проверки у заказа есть подписанный JWS — <c>order.Info.Apple.jwsRepresentation</c>,
		/// он уходит в App Store Server API v2. У Unity есть и свой сервис проверки транзакций
		/// (сборка <c>Unity.Purchasing.TransactionVerifier</c> под дефайном <c>IAP_TX_VERIFIER_ENABLED</c>), он не включён.
		/// </para>
		/// Поэтому <see cref="UnityPurchasingSettings.appleDisableValidationRecipe"/> ни на что не влияет
		/// </remarks>
		private void TryInitializeLocalValidator()
		{
			if (_settings.disableValidationRecipe)
				return;

			byte[] googlePlayData = null;

#if APP_GOOGLE_PLAY
			if (_billing == IAPBillingType.GOOGLE_PLAY)
			{
				if (_settings.googlePlayDisableValidationRecipe)
					return;

				googlePlayData = GooglePlayTangle.Data();
			}
#endif
			// Без ключей Google Play валидировать нечего: App Store сюда не попадает (см. remarks)
			if (googlePlayData == null)
				return;

			_localValidator = new CrossPlatformValidator(googlePlayData, _appIdentifier);
		}

		private IAPBillingEntry GetDefaultBilling(in DistributionEntry platform)
		{
			switch (platform)
			{
				case DistributionType.APP_STORE:
					return IAPBillingType.APP_STORE;
				case DistributionType.GOOGLE_PLAY:
					return IAPBillingType.GOOGLE_PLAY;
				default:
					return IAPBillingType.UNDEFINED;
			}
		}

#if UNITY_EDITOR
		[MenuItem("GameObject/In-App Purchasing/IAP Listener", true)]
		public static bool GameObjectDisableCreateIAPListener() => false;

		[MenuItem("GameObject/In-App Purchasing/IAP Button", true)]
		public static bool GameObjectDisableCreateIAPButton() => false;
#endif
	}
}
