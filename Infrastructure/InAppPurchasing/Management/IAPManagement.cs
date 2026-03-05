#if DebugLog
#define IAP_DEBUG
#endif
using System;
using System.Threading;
using System.Threading.Tasks;
using Content;

namespace InAppPurchasing
{
	public enum ProductStatus
	{
		/// <summary>
		/// Не определенный статус продукта
		/// </summary>
		Undefined,

		Available,
		NotAvailable,

		AlreadyProcessing,
		Deferred,

		/// <summary>
		/// Только Non-Consumable и Subscription
		/// </summary>
		Purchased,

		NotInitialized,
		Unknown,
	}

	public partial class IAPManagement : IDisposable
	{
		private IInAppPurchasingIntegration _integration;

		private IInAppPurchasingService _service;

		private readonly ProductInfo _emptyProductInfo = default;
		private readonly SubscriptionInfo _emptySubscriptionInfo = default;

		private readonly InAppPurchasingRelay _relay;

		internal IInAppPurchasingEvents Events => _relay;
		internal IInAppPurchasingIntegration Integration => _integration;

		public IAPManagement(IInAppPurchasingIntegration integration)
		{
			_relay = new InAppPurchasingRelay();

			SetIntegration(integration);
		}

		public void Dispose() => _relay.Dispose();

		#region Service

		internal DateTime DateTime { get => _service.DateTime; }

		internal void Bind(IInAppPurchasingService service)
		{
			Unbind();
			_service = service;
		}

		internal void Unbind()
		{
			_service = null;
		}

		internal void RegisterReceipt(in PurchaseReceipt receipt) => _service.Register(in receipt);

		internal bool ContainsReceipt(string transactionId)
		{
			return _service.Contains(transactionId);
		}

		#endregion

		internal ref readonly ProductInfo GetProductInfo<T>(string product, bool forceUpdateCache = false)
			where T : IAPProductEntry
		{
			if (!ContentManager.Contains<T>(product))
				return ref _emptyProductInfo;

			var entry = ContentManager.Get<IAPSubscriptionProductEntry>(product);
			return ref GetProductInfo(entry, forceUpdateCache);
		}

		internal ref readonly ProductInfo GetProductInfo<T>(T entry, bool forceUpdateCache = false)
			where T : IAPProductEntry =>
			ref _integration.GetProductInfo(entry, forceUpdateCache);

		internal IAPProductEntry GetEntry(IAPProductType type, string product)
			=> type switch
			{
				IAPProductType.Consumable => ContentManager.Get<IAPConsumableProductEntry>(product),
				IAPProductType.NonConsumable => ContentManager.Get<IAPNonConsumableProductEntry>(product),
				IAPProductType.Subscription => ContentManager.Get<IAPSubscriptionProductEntry>(product),
				_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
			};

		#region Can Purchase

		internal bool CanPurchase<T>(string product, out IAPPurchaseError? error) where T : IAPProductEntry
		{
			if (!ContentManager.Contains<T>(product))
			{
				error = IAPPurchaseErrorCode.ProductEntryNotFound;
				return false;
			}

			var entry = ContentManager.Get<T>(product);
			return CanPurchase(entry, out error);
		}

		internal bool CanPurchase(IAPProductType type, string product, out IAPPurchaseError? error)
		{
			IAPProductEntry entry;
			switch (type)
			{
				case IAPProductType.Consumable:
					entry = ContentManager.Get<IAPConsumableProductEntry>(product);
					return CanPurchase(entry, out error);
				case IAPProductType.NonConsumable:
					entry = ContentManager.Get<IAPNonConsumableProductEntry>(product);
					return CanPurchase(entry, out error);
				case IAPProductType.Subscription:
					entry = ContentManager.Get<IAPSubscriptionProductEntry>(product);
					return CanPurchase(entry, out error);
			}

			error = new IAPPurchaseError(IAPPurchaseErrorCode.ProductTypeNotImplemented);
			return false;
		}

		internal bool CanPurchase(IAPProductEntry entry, out IAPPurchaseError? error)
		{
			switch (entry.Type)
			{
				case IAPProductType.Consumable:
					return _integration.CanPurchaseConsumable(entry, out error);
				case IAPProductType.NonConsumable:
					return _integration.CanPurchaseNonConsumable(entry, out error);
				case IAPProductType.Subscription:
					return _integration.CanPurchaseSubscription(entry, out error);
			}

			error = new IAPPurchaseError(IAPPurchaseErrorCode.ProductTypeNotImplemented);
			return false;
		}

		#endregion

		#region Purchase

		internal bool RequestPurchase<T>(string product) where T : IAPProductEntry
		{
			if (!ContentManager.Contains<T>(product))
				return false;

			var entry = ContentManager.Get<T>(product);
			return RequestPurchase(entry);
		}

		internal bool RequestPurchase(IAPProductType type, string product)
		{
			IAPProductEntry entry = type switch
			{
				IAPProductType.Consumable => ContentManager.Get<IAPConsumableProductEntry>(product),
				IAPProductType.NonConsumable => ContentManager.Get<IAPNonConsumableProductEntry>(product),
				IAPProductType.Subscription => ContentManager.Get<IAPSubscriptionProductEntry>(product),
				_ => null
			};

			return entry != null && RequestPurchase(entry);
		}

		internal bool RequestPurchase(IAPProductEntry entry)
		{
			return entry.Type switch
			{
				IAPProductType.Consumable => _integration.RequestPurchaseConsumable(entry),
				IAPProductType.NonConsumable => _integration.RequestPurchaseNonConsumable(entry),
				IAPProductType.Subscription => _integration.RequestPurchaseSubscription(entry),
				_ => false
			};
		}

		#endregion

		#region Purchase Async

		internal Task<PurchaseResult> PurchaseAsync<T>(string product, CancellationToken cancellationToken) where T : IAPProductEntry
		{
			if (!ContentManager.Contains<T>(product))
				return Task.FromResult(PurchaseResult.Failed);

			var entry = ContentManager.Get<T>(product);
			return PurchaseAsync(entry, cancellationToken);
		}

		internal Task<PurchaseResult> PurchaseAsync(IAPProductType type, string product, CancellationToken cancellationToken)
		{
			IAPProductEntry entry = type switch
			{
				IAPProductType.Consumable => ContentManager.Get<IAPConsumableProductEntry>(product),
				IAPProductType.NonConsumable => ContentManager.Get<IAPNonConsumableProductEntry>(product),
				IAPProductType.Subscription => ContentManager.Get<IAPSubscriptionProductEntry>(product),
				_ => null
			};

			return entry ? PurchaseAsync(entry, cancellationToken) : Task.FromResult(PurchaseResult.Failed);
		}

		internal async Task<PurchaseResult> PurchaseAsync(IAPProductEntry entry, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<PurchaseResult>(TaskCreationOptions.RunContinuationsAsynchronously);

			// ReSharper disable once UseAwaitUsing
			using (cancellationToken.Register(Cancel))
			{
				// Это на случай если сервис поменяют, так как такой функционал есть
				var service = _integration;

				try
				{
					service.PurchaseCompleted += OnCompleted;
					service.PurchaseFailed += OnFailed;
					service.PurchaseCanceled += OnCanceled;

					var success = entry.Type switch
					{
						IAPProductType.Consumable => service.RequestPurchaseConsumable(entry),
						IAPProductType.NonConsumable => service.RequestPurchaseNonConsumable(entry),
						IAPProductType.Subscription => service.RequestPurchaseSubscription(entry),
						_ => false
					};

					if (!success)
						return PurchaseResult.Failed;

					return await tcs.Task; //.ConfigureAwait(false); можно вне Unity)
				}
				catch (Exception e)
				{
					IAPDebug.LogException(e);
					return PurchaseResult.Failed;
				}
				finally
				{
					service.PurchaseCompleted -= OnCompleted;
					service.PurchaseFailed -= OnFailed;
					service.PurchaseCanceled -= OnCanceled;
				}

				void OnCompleted(in PurchaseReceipt receipt, bool processing, object rawData)
				{
					if (!processing || receipt.ToEntry() != entry)
						return;

					tcs.TrySetResult(new PurchaseResult(true)
					{
						receipt = receipt,
						rawData = rawData
					});
				}

				void OnFailed(IAPProductEntry product, string error, object rawData)
				{
					if (product != entry)
						return;

					tcs.TrySetResult(PurchaseResult.Failed);
				}

				void OnCanceled(IAPProductEntry product, object rawData)
				{
					if (product != entry)
						return;

					tcs.TrySetResult(PurchaseResult.Canceled);
				}
			}

			void Cancel() => tcs.TrySetCanceled(cancellationToken);
		}

		#endregion

		#region Get Status

		internal ProductStatus GetStatus<T>(string product)
			where T : IAPProductEntry
		{
			if (!ContentManager.Contains<T>(product))
				return ProductStatus.Unknown;

			var entry = ContentManager.Get<T>(product);
			return GetStatus(entry);
		}

		internal ProductStatus GetStatus(IAPProductType type, string product)
		{
			IAPProductEntry entry = type switch
			{
				IAPProductType.Consumable => ContentManager.Get<IAPConsumableProductEntry>(product),
				IAPProductType.NonConsumable => ContentManager.Get<IAPNonConsumableProductEntry>(product),
				IAPProductType.Subscription => ContentManager.Get<IAPSubscriptionProductEntry>(product),
				_ => null
			};

			return entry == null ? ProductStatus.Unknown : GetStatus(entry);
		}

		internal ProductStatus GetStatus(IAPProductEntry product)
		{
			if (_integration.TryGetStatus(product, out var status))
				return status;

			return ProductStatus.Available;
		}

		#endregion

		#region Restore

		internal bool IsRestoreTransactionsSupported() => _integration.IsRestoreTransactionsSupported;
		internal void RestoreTransactions() => _integration.RestoreTransactions();

		#endregion

		#region Subscription

		internal ref readonly SubscriptionInfo GetSubscriptionInfo(string product, bool forceUpdateCache = false)
		{
			if (!ContentManager.Contains<IAPSubscriptionProductEntry>(product))
				return ref _emptySubscriptionInfo;

			var entry = ContentManager.Get<IAPSubscriptionProductEntry>(product);
			return ref GetSubscriptionInfo(entry, forceUpdateCache);
		}

		internal ref readonly SubscriptionInfo GetSubscriptionInfo(IAPSubscriptionProductEntry entry, bool forceUpdateCache = false)
		{
			return ref _integration.GetSubscriptionInfo(entry, forceUpdateCache);
		}

		#endregion

		internal PurchaseReceipt? GetPurchaseReceipt(string transactionId)
		{
			return _service.GetReceipt(transactionId);
		}

		internal IInAppPurchasingIntegration SetIntegration(IInAppPurchasingIntegration integration)
		{
			var prev = _integration;
#if IAP_DEBUG
			if (_integration?.GetType() == integration.GetType())
			{
				IAPDebug.LogWarning($"Same integration: {_integration.Name}");
				return prev;
			}
#endif
			_integration = integration;

			_relay.Bind(_integration);

			IAPDebug.Log($"Target integration: {_integration.Name}");

			return prev;
		}
	}

	public struct PurchaseResult
	{
		public bool success;

		public PurchaseErrorCode errorCode;

		public PurchaseReceipt receipt;

		public object rawData;

		public PurchaseResult(bool success) : this()
		{
			this.success = success;
		}

		public static PurchaseResult Failed = new(false)
		{
			errorCode = PurchaseErrorCode.Failed
		};

		public static PurchaseResult Canceled = new(false)
		{
			errorCode = PurchaseErrorCode.Canceled
		};
	}

	public enum PurchaseErrorCode
	{
		None,

		Failed,
		Canceled
	}
}
