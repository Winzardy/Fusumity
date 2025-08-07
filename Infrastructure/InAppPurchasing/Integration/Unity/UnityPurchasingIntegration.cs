#if (UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_TVOS) && !UNITY_EDITOR
#define APP_STORE
#endif

#if (UNITY_ANDROID) && !UNITY_EDITOR
#define APP_GOOGLE_PLAY
#endif

using System;
using System.Collections.Generic;
using System.Threading;
using Targeting;
using Content;
using Cysharp.Threading.Tasks;
using Fusumity.Utility;
using Fusumity.Utility.UserLocator;
using JetBrains.Annotations;
using Sapientia.Collections;
using Sapientia.Extensions;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;
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
		/// Включает или выключает режим симуляции отложенных покупок (Ask-to-Buy) в Apple App Store (полезно для тестирования)
		/// </summary>
		public bool appleSimulateAskToBuy;

		/// <summary>
		/// Включает или выключает проверку чеков Apple во время обработки покупок
		/// </summary>
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

		public Dictionary<DistributionEntry, Dictionary<CountryEntry, IAPBillingEntry>> storeToCountryToBilling;
	}

	public partial class UnityPurchasingIntegration : IInAppPurchasingIntegration, IDetailedStoreListener, IDisposable
	{
		public string Name => "UnityPurchasing";

		private readonly string _appIdentifier;

		private readonly UnityPurchasingSettings _settings;
		private readonly DistributionEntry _distributionPlatform;
		private IAPBillingEntry _billing;

		private IStoreController _storeController;
		private IExtensionProvider _extensions;

		[CanBeNull]
		private IGooglePlayStoreExtensions _googlePlayExtension;

		//TODO: переключение магазина на Android при билде через TeamCity
		/// <summary>
		/// Пока не поддерживается, требует переключения магазина при билде
		/// <code>UnityPurchasingEditor.TargetAndroidStore(AndroidStore.AmazonAppStore)</code>
		/// </summary>
		[CanBeNull]
		private IAmazonExtensions _amazonExtension;

		[CanBeNull]
		private IAppleExtensions _appleExtension;

		/// <summary>
		/// Валидатор чеков, работает только для Google Play и App Store
		/// </summary>
		[CanBeNull]
		private CrossPlatformValidator _validator;

		private UniTaskCompletionSource<UnityPurchasingInitializationFailureReason> _initializationCompletionSource;

		#region Apple Configuration

		private string _appleAppReceipt;
		private bool _appleCanMakePayments;

		#endregion

		/// <summary>
		/// Список продуктов, которые в данный момент обрабатываются (Billing Product ID)
		/// </summary>
		private HashSet<string> _processing;

		/// <summary>
		/// Магазинный ID продукта - Запись продукта. Важно понимать что ID для разных платформ может отличаться!
		/// </summary>
		private BidirectionalMap<string, IAPProductEntry> _billingProductIdToEntry;

		/// <summary>
		/// Нужен чтобы контролировать из вне когда "продолжить" покупки из промо (пока только для app store)
		/// </summary>
		private readonly UniTaskCompletionSource _storePromotionalCompletionSource;

		private readonly IInAppPurchasingService _service;
		private IInAppPurchasingGrantCenter _grantCenter;

		public bool IsInitialized => _storeController != null && _processing != null;

		public event PurchaseCompleted PurchaseCompleted;
		public event PurchaseFailed PurchaseFailed;
		public event PurchaseRequested PurchaseRequested;
		public event PurchaseCanceled PurchaseCanceled;
		public event PurchaseDeferred PurchaseDeferred;
		public event PromotionalPurchaseIntercepted PromotionalPurchaseIntercepted;

		public UnityPurchasingIntegration(IInAppPurchasingService service,
			IInAppPurchasingGrantCenter grantCenter,
			in UnityPurchasingSettings settings,
			in DistributionEntry distributionPlatform,
			string appIdentifier,
			UniTaskCompletionSource storePromotionalCompletionSource = null)
		{
			_service = service;
			_grantCenter = grantCenter;
			_settings = settings;
			_distributionPlatform = distributionPlatform;
			_appIdentifier = appIdentifier;
			_storePromotionalCompletionSource = storePromotionalCompletionSource;
		}

		public void Dispose()
		{
			UnityPurchasingUtility.appleExtensions = null;
		}

		public async UniTask<UnityPurchasingInitializationFailureReason> InitializeAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				var success = await UnityServices.UnityServiceInitializationAsync(cancellationToken);

				if (!success)
					return UnityPurchasingInitializationFailureReason.UnityServices;

				var autoSetDefaultBilling = true;

				if (_settings.storeToCountryToBilling.TryGetValue(_distributionPlatform, out var countryToBilling))
				{
					var country = await UserLocator.GetCountryAsync(cancellationToken);

					if (country == UserLocator.UNDEFINED)
						return UnityPurchasingInitializationFailureReason.UnknownCountry;

					if (countryToBilling.TryGetValue(country, out var billing))
					{
						_billing = billing;
						autoSetDefaultBilling = false;
					}
				}

				if (autoSetDefaultBilling)
					_billing = GetDefaultBilling(_distributionPlatform);

#if !UNITY_EDITOR
				if (_billing == IAPBillingType.UNDEFINED)
					return UnityPurchasingInitializationFailureReason.UnknownPlatform;
#endif

				var module = StandardPurchasingModule.Instance();
				var builder = ConfigurationBuilder.Instance(module);

				_billingProductIdToEntry = new BidirectionalMap<string, IAPProductEntry>(4);

				AddProductsToBuilder<IAPConsumableProductEntry>(builder);
				AddProductsToBuilder<IAPNonConsumableProductEntry>(builder);
				AddProductsToBuilder<IAPSubscriptionProductEntry>(builder);

				if (_billing == IAPBillingType.GOOGLE_PLAY)
				{
					var configuration = builder.Configure<IGooglePlayConfiguration>();
					configuration.SetDeferredPurchaseListener(OnDeferredPurchase);
				}

				if (_billing == IAPBillingType.APP_STORE)
				{
					var configuration = builder.Configure<IAppleConfiguration>();
					_appleCanMakePayments = configuration.canMakePayments;
					_appleAppReceipt = configuration.appReceipt;
					configuration.SetApplePromotionalPurchaseInterceptorCallback(OnApplePromotionalPurchaseInterceptor);

					// Данный метод может обрабатывать отозванный продукты (family share)
					//configuration.SetEntitlementsRevokedListener(EntitlementsRevokeListener);
				}

				_initializationCompletionSource = new UniTaskCompletionSource<UnityPurchasingInitializationFailureReason>();

				var productsStr = builder.products
				   .GetCompositeString(true, definition => definition.storeSpecificId);
				IAPDebug.Log($"UnityPurchasing initializing, billing: {_billing}, products:{productsStr}");

				UnityPurchasing.Initialize(this, builder);
				return await _initializationCompletionSource.Task;
			}
			catch (Exception ex)
			{
				IAPDebug.LogException(ex);
				return UnityPurchasingInitializationFailureReason.Exception;
			}
		}

		void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
			var productsStr = controller.products
			   .set
			   .GetCompositeString(true, product => product.definition.storeSpecificId);
			IAPDebug.Log($"UnityPurchasing successfully initialized, billing: {_billing}, products:{productsStr}");

			_processing = new(2);

			_initializationCompletionSource.TrySetResult(UnityPurchasingInitializationFailureReason.None);

			_storeController = controller;
			_extensions = extensions;

			switch (_billing)
			{
				case IAPBillingType.GOOGLE_PLAY:
					_googlePlayExtension = extensions.GetExtension<IGooglePlayStoreExtensions>();
					break;

				case IAPBillingType.APP_STORE:
					_appleExtension = _extensions.GetExtension<IAppleExtensions>();

					UnityPurchasingUtility.appleExtensions = _appleExtension;

					if (_appleExtension != null)
					{
						_appleExtension.RegisterPurchaseDeferredListener(OnDeferredPurchase);
						_appleExtension.simulateAskToBuy = _settings.appleSimulateAskToBuy;
					}

					break;

				case IAPBillingType.AMAZON:
					_amazonExtension = _extensions.GetExtension<IAmazonExtensions>();
					break;
			}

			TryInitializeValidator();

			//Аддитивно добавлять продукты после инициализации, если вдруг это будет нужно
			//_controller.FetchAdditionalProducts();
		}

		public bool TryGetStatus(IAPProductEntry product, out ProductStatus status)
		{
			status = ProductStatus.Undefined;

			if (!IsInitialized)
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

			if (unityProduct.IsPurchased())
			{
				status = ProductStatus.Purchased;
				return true;
			}

			if (IsDeferred(unityProduct))
			{
				status = ProductStatus.Deferred;
				return true;
			}

			return false;
		}

		#region Restore

		public bool IsRestoreTransactionsSupported => _appleExtension != null;

		public void RestoreTransactions()
		{
			if (_appleExtension == null)
				return;

			_appleExtension.RestoreTransactions(OnRestoredTransactions);

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

			if (unityProduct.IsPurchased())
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
			product = _storeController?.products.WithID(billingProductId);
			return product != null;
		}

		private bool CanPurchase(IAPProductEntry product, out IAPPurchaseError? error)
		{
			error = null;

			if (!IsInitialized)
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

			if (!_processing.Add(billingProductId))
				return false;

			var entry = _billingProductIdToEntry[billingProductId];
			PurchaseRequested?.Invoke(entry);

			_storeController.InitiatePurchase(billingProductId);
			return true;
		}

		#region Store Listener

		void IStoreListener.OnInitializeFailed(InitializationFailureReason error)
		{
			_initializationCompletionSource.TrySetResult(error.Convert());

			IAPDebug.LogError($"Failed to initialize: {error}");
		}

		void IStoreListener.OnInitializeFailed(InitializationFailureReason error, string message)
		{
			_initializationCompletionSource.TrySetResult(error.Convert());

			IAPDebug.LogError($"Failed to initialize: {error} {message}");
		}

		PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs args)
		{
			var product = args.purchasedProduct;

			var billingProductId = product.definition.id;
			var isDeferred = false;
			if (IsDeferred(product))
			{
				_processing.Remove(billingProductId);
				isDeferred = true;
			}

			if (!_billingProductIdToEntry.TryGetValue(billingProductId, out var entry))
			{
				_processing.Remove(product.definition.id);
				IAPDebug.LogError($"[{entry.Type}] Failed to purchase: Not found product by product id [ {billingProductId} ]");
				return PurchaseProcessingResult.Pending;
			}

			if (isDeferred)
			{
				PurchaseDeferred?.Invoke(entry);
				return PurchaseProcessingResult.Pending;
			}

			var isRestored = product.appleProductIsRestored;
			if (isRestored)
			{
				IAPDebug.Log(
					$"[{entry.Type}] restore purchase detected for product id [ {billingProductId} ] by transaction id [ {product.transactionID} ]");

				// Отдельная логика для восстановления (если нужна)
			}

			var transactionId = product.transactionID;
			if (transactionId.IsNullOrEmpty())
			{
				_processing.Remove(product.definition.id);
				IAPDebug.LogError($"[{entry.Type}] Failed to purchase: empty transaction id " +
					$"for product id [ {billingProductId} ] (billing: {_billing})");
				return PurchaseProcessingResult.Complete;
			}

			if (_service.Contains(transactionId))
			{
				_processing.Remove(product.definition.id);
				IAPDebug.LogError($"[{entry.Type}] Failed to purchase: Transaction by id [ {transactionId} ] has already been completed " +
					$"for product id [ {billingProductId} ] (billing: {_billing})");
				return PurchaseProcessingResult.Complete;
			}

			// Можно сделать режимы валидации чека (клиент, сервер, гибрид), но пока клиент
			if (!ValidateReceipt(product))
			{
				OnPurchaseFailedInternal(entry, "Invalid receipt", args);
				return PurchaseProcessingResult.Complete;
			}

			var unityProduct = args.purchasedProduct;
			try
			{
				var receipt = new PurchaseReceipt
				{
					productType = entry.Type,
					productId = entry.Id,

					billing = _billing,

					transactionId = transactionId,
					receipt = product.receipt,

					isRestored = isRestored
				};

				if (_processing.Remove(billingProductId))
				{
					Complete(receipt, true);
					return PurchaseProcessingResult.Complete;
				}

				receipt.isRestored = true;
				_grantCenter.Grant(in receipt, OnComplete);
				return PurchaseProcessingResult.Pending;
			}
			catch (Exception e)
			{
				OnPurchaseFailedInternal(entry, e.Message, args);
				return PurchaseProcessingResult.Pending;
			}

			void OnComplete(in PurchaseReceipt receipt) =>
				Complete(in receipt, false);

			void Complete(in PurchaseReceipt receipt, bool live)
			{
				_storeController.ConfirmPendingPurchase(unityProduct);
				if (live)
					_service.Register(in receipt);
				PurchaseCompleted?.Invoke(in receipt, live, args);
			}
		}

		/// <returns><c>true</c> Если покупка отложена (работает пока только в Google Play),
		/// <c>false</c> - на данный момент неизвестно отложенная покупка или нет</returns>
		private bool IsDeferred(UnityProduct product)
		{
			if (_googlePlayExtension != null && _googlePlayExtension.IsPurchasedProductDeferred(product))
				return true;

			return false;
		}

		private bool ValidateReceipt(UnityProduct product)
		{
			const string PREFIX = "[ Validation ]";
			const int TOLERANCE_MINUTES = 5;

#if UNITY_EDITOR
			if (_validator == null)
				return true;
#endif

			try
			{
				var receipts = _validator.Validate(product.receipt);

				if (receipts.IsNullOrEmpty())
				{
					IAPDebug.LogError($"{PREFIX} Receipt is null or empty for product id [ {product.definition.id} ]");
					return false;
				}

				foreach (var receipt in receipts)
				{
					if (receipt.productID != product.definition.id)
					{
						IAPDebug.LogError(
							$"{PREFIX} Product ID mismatch: receipt = {receipt.productID}, expected = {product.definition.id}");
						return false;
					}

					if (product.definition.type != ProductType.Consumable)
					{
						if (receipt.purchaseDate == DateTime.MinValue)
						{
							IAPDebug.LogError(
								$"{PREFIX} Missing purchase date for non-consumable product [ {product.definition.id} ]");
							return false;
						}

						var purchaseDate = receipt.purchaseDate;
						var nowDate = _service.GetUtcNow();
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
					$"{PREFIX} Failed to validate: Exception for product by store product id [ {product.definition.id} ] " +
					$"(type: {product.definition.type}): {e.Message}");
				return false;
			}
		}

		void IStoreListener.OnPurchaseFailed(UnityProduct product, PurchaseFailureReason reason)
		{
			switch (reason)
			{
				case PurchaseFailureReason.UserCancelled:
					OnPurchaseCanceledInternal(product, reason);
					break;

				default:
					OnPurchaseFailedInternal(product, reason.ToString(), reason);
					break;
			}
		}

		private void OnPurchaseFailedInternal(UnityProduct product, string error, object rawData = null)
		{
			var billingProductId = product.definition.id;
			if (_billingProductIdToEntry.TryGetValue(billingProductId, out var entry))
				OnPurchaseFailedInternal(entry, error, rawData);
			else
				IAPDebug.LogError(
					$"[{product.definition.type}] Failed to purchase product (not found!) by store product id [ {billingProductId} ]: {error}");
		}

		private void OnPurchaseCanceledInternal(UnityProduct product, object rawData = null)
		{
			var billingProductId = product.definition.id;
			if (_billingProductIdToEntry.TryGetValue(billingProductId, out var entry))
				OnPurchaseCanceledInternal(entry, rawData);
			else
				IAPDebug.LogError(
					$"[{product.definition.type}] Failed to canceled product (not found!) by store product id [ {billingProductId} ]");
		}

		void IDetailedStoreListener.OnPurchaseFailed(UnityProduct product, PurchaseFailureDescription failureDescription)
			=> OnPurchaseFailedInternal(product, failureDescription.message, failureDescription);

		private void OnDeferredPurchase(UnityProduct product)
		{
			var billingProductId = product.definition.id;
			if (_billingProductIdToEntry.TryGetValue(billingProductId, out var entry))
				PurchaseDeferred?.Invoke(entry, product);
			else
				IAPDebug.LogError(
					$"[{product.definition.type}] Failed to deferred product (not found!) by store product id [ {billingProductId} ]");
		}

		private void OnApplePromotionalPurchaseInterceptor(UnityProduct product)
		{
			if (!_billingProductIdToEntry.TryGetValue(product.definition.id, out var entry))
			{
				IAPDebug.LogError($"[{product.definition.type}] Failed to apple promotional purchase: Not found product by id [ " +
					product.definition.id + " ]");
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
			_appleExtension!.ContinuePromotionalPurchases();
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

		private void AddProductsToBuilder<TProduct>(ConfigurationBuilder builder)
			where TProduct : IAPProductEntry
		{
			foreach (var entry in ContentManager.GetAll<TProduct>())
			{
				ref readonly var product = ref entry.Value;
				var id = product.GetBillingId(in _billing);
				_billingProductIdToEntry[id] = product;
				builder.AddProduct(id, product.ToUnityProductType());
			}
		}

		private void TryInitializeValidator()
		{
			if (_settings.disableValidationRecipe)
				return;

			byte[] googlePlayData = null;
			byte[] appleData = null;

#if APP_GOOGLE_PLAY
			if (_billing == DistributionType.GOOGLE_PLAY)
			{
				if (_settings.googlePlayDisableValidationRecipe)
					return;

				googlePlayData = GooglePlayTangle.Data();
			}
#endif

#if APP_STORE
			if (_billing == DistributionType.APP_STORE)
			{
				if (_settings.appleDisableValidationRecipe)
					return;

				appleData = AppleTangle.Data();
			}
#endif
			if (googlePlayData == null && appleData == null)
				return;

			_validator = new CrossPlatformValidator(googlePlayData, appleData, _appIdentifier);
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
		[MenuItem("Services/In-App Purchasing/Create IAP Button", true)]
		public static bool DisableCreateIAPButton() => false;

		[MenuItem("Services/In-App Purchasing/Create IAP Button (Legacy)", true)]
		public static bool DisableCreateIAPButtonLegacy() => false;

		[MenuItem("Services/In-App Purchasing/Create IAP Listener", true)]
		public static bool DisableCreateIAPListener() => false;

		[MenuItem("Services/In-App Purchasing/IAP Catalog...", true)]
		public static bool DisableCreateIAPCatalog() => false;

		[MenuItem("GameObject/In-App Purchasing/IAP Button (Legacy)", true)]
		public static bool GameObjectDisableCreateIAPButtonLegacy() => false;

		[MenuItem("GameObject/In-App Purchasing/IAP Listener", true)]
		public static bool GameObjectDisableCreateIAPListener() => false;

		[MenuItem("GameObject/In-App Purchasing/IAP Button", true)]
		public static bool GameObjectDisableCreateIAPButton() => false;
#endif
	}
}
