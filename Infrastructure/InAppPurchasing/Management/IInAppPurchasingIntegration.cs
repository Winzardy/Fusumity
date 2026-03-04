using System;

namespace InAppPurchasing
{
	public interface IInAppPurchasingIntegration : IInAppPurchasingEvents
	{
		public bool TryGetStatus(IAPProductEntry product, out ProductStatus status);

		public bool IsRestoreTransactionsSupported { get; }

		/// <summary>
		/// Only Apple App Store
		/// </summary>
		public void RestoreTransactions();

		public ref readonly ProductInfo GetProductInfo(IAPProductEntry entry, bool forceUpdateCache = false);

		#region Consumable

		public bool CanPurchaseConsumable(IAPProductEntry product, out IAPPurchaseError? error);
		public bool RequestPurchaseConsumable(IAPProductEntry entry);

		#endregion

		#region NonConsumable

		public bool CanPurchaseNonConsumable(IAPProductEntry entry, out IAPPurchaseError? error);
		public bool RequestPurchaseNonConsumable(IAPProductEntry entry);

		#endregion

		#region Subscription

		public bool CanPurchaseSubscription(IAPProductEntry entry, out IAPPurchaseError? error);
		public bool RequestPurchaseSubscription(IAPProductEntry entry);
		public ref readonly SubscriptionInfo GetSubscriptionInfo(IAPSubscriptionProductEntry subscription, bool forceUpdateCache = false);

		#endregion

		string Name { get; }
	}

	public readonly struct IAPPurchaseError : IEquatable<IAPPurchaseError>
	{
		public readonly IAPPurchaseErrorCode code;

		/// <summary>
		/// Сырые данные из интеграции
		/// </summary>
#if CLIENT
		[JetBrains.Annotations.CanBeNull]
#endif
		public readonly object rawData;

		public IAPPurchaseError(IAPPurchaseErrorCode code, object rawData = null)
		{
			this.code = code;
			this.rawData = rawData;
		}

		public static implicit operator IAPPurchaseError(IAPPurchaseErrorCode code) => new IAPPurchaseError(code);
		public static implicit operator IAPPurchaseErrorCode(IAPPurchaseError error) => error.code;

		public override string ToString() => rawData == null ? code.ToString() : $"{code} {rawData}";

		public bool Equals(IAPPurchaseError other) => code == other.code && Equals(rawData, other.rawData);

		public override bool Equals(object obj) => obj is IAPPurchaseError other && Equals(other);

		public override int GetHashCode() => HashCode.Combine((int) code, rawData);
	}

	public enum IAPPurchaseErrorCode
	{
		None,

		NotInitialized,
		ProductEntryNotFound,
		InvalidProductType,

		InProgress,

		//Non-Consumable и Subscription
		Purchased,

		ProductTypeNotImplemented,
		ProductNotFoundInService,
	}

	public interface IInAppPurchasingEvents
	{
		public event PurchaseCompleted PurchaseCompleted;

		public event PurchaseFailed PurchaseFailed;
		public event PurchaseRequested PurchaseRequested;
		public event PurchaseCanceled PurchaseCanceled;

		public event PurchaseDeferred PurchaseDeferred;

		/// <summary>
		/// Перехватить Promotional покупку (такое пока только в <see href="https://docs.unity3d.com/Packages/com.unity.purchasing@4.12/api/UnityEngine.Purchasing.IAppleConfiguration.html" langword="external">Apple</see>)
		/// </summary>
		public event PromotionalPurchaseIntercepted PromotionalPurchaseIntercepted;
	}

	public enum IAPProductType
	{
		Consumable,
		NonConsumable,
		Subscription
	}

	#region Delegates

	/// <param name="live">
	/// <c>true</c>, если покупка завершилась в момент запроса (live);
	/// <c>false</c>, если она была восстановлена или отложена (например, Restore или Deferred).
	/// </param>
	public delegate void PurchaseCompleted(in PurchaseReceipt receipt, bool live, object rawData = null);

	public delegate void PurchaseFailed(IAPProductEntry product, string error, object rawData = null);

	public delegate void PurchaseRequested(IAPProductEntry product);

	public delegate void PurchaseCanceled(IAPProductEntry product, object rawData = null);

	/// <summary>
	/// Сообщает о том, что покупка была отложена, не покупка!
	/// </summary>
	public delegate void PurchaseDeferred(IAPProductEntry product, object rawData = null);

	public delegate void PromotionalPurchaseIntercepted(IAPProductEntry product, object rawData = null);

	#endregion

	public struct PurchaseReceipt
	{
		public IAPProductType productType;
		public string productId;

		public IAPBillingEntry billing;

		public string transactionId;
		public string receipt;

		public bool isRestored;
	}
}
