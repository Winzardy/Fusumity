#if DebugLog
#define IAP_DEBUG
#endif
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Sapientia;

namespace InAppPurchasing
{
	public partial class IAPManager : StaticWrapper<IAPManagement>
	{
		private static IAPManagement management
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		public static bool IsInitialized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance != null;
		}

		public static IInAppPurchasingEvents Events => management.Events;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanPurchase<T>(string product, out IAPPurchaseError? error)
			where T : IAPProductEntry
			=> management.CanPurchase<T>(product, out error);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanPurchase(IAPProductEntry entry, out IAPPurchaseError? error)
			=> management.CanPurchase(entry, out error);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanPurchase(IAPProductType type, string product, out IAPPurchaseError? error)
			=> management.CanPurchase(type, product, out error);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IAPProductEntry GetEntry(IAPProductType type, string product)
			=> management.GetEntry(type, product);

		#region Purchase

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool RequestPurchase<T>(string product)
			where T : IAPProductEntry
			=> management.RequestPurchase<T>(product);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool RequestPurchase(IAPProductEntry entry)
			=> management.RequestPurchase(entry);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool RequestPurchase(IAPProductType type, string product)
			=> management.RequestPurchase(type, product);

		#endregion

		#region Purchase Async

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<PurchaseResult> PurchaseAsync(IAPProductType type, string product, CancellationToken cancellationToken)
			=> management.PurchaseAsync(type, product, cancellationToken);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<PurchaseResult> PurchaseAsync<T>(string product, CancellationToken cancellationToken)
			where T : IAPProductEntry
			=> management.PurchaseAsync<T>(product, cancellationToken);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<PurchaseResult> PurchaseAsync(IAPProductEntry entry, CancellationToken cancellationToken)
			=> management.PurchaseAsync(entry, cancellationToken);

		#endregion

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ProductStatus GetStatus(IAPProductEntry entry) => management.GetStatus(entry);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ProductStatus GetStatus<T>(string product) where T : IAPProductEntry
			=> management.GetStatus<T>(product);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ProductStatus GetStatus(IAPProductType type, string product) => management.GetStatus(type, product);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsRestoreSupported() => management.IsRestoreTransactionsSupported();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void RestoreTransactions() => management.RestoreTransactions();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref readonly SubscriptionInfo GetSubscriptionInfo(string product, bool forceUpdateCache = false)
			=> ref management.GetSubscriptionInfo(product, forceUpdateCache);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref readonly SubscriptionInfo GetSubscriptionInfo(IAPSubscriptionProductEntry entry, bool forceUpdateCache = false)
			=> ref management.GetSubscriptionInfo(entry, forceUpdateCache);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref readonly ProductInfo GetProductInfo<T>(string product, bool forceUpdateCache = false)
			where T : IAPProductEntry
			=> ref management.GetProductInfo<T>(product, forceUpdateCache);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref readonly ProductInfo GetProductInfo<T>(T entry, bool forceUpdateCache = false)
			where T : IAPProductEntry
			=> ref management.GetProductInfo(entry, forceUpdateCache);

		public static void Bind(IInAppPurchasingService service) => management.Bind(service);
		public static void Unbind() => management.Unbind();

		#region Service

		public static DateTime DateTime { get => management.DateTime; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static PurchaseReceipt? GetReceipt(string transactionId) => management.GetPurchaseReceipt(transactionId);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void RegisterReceipt(in PurchaseReceipt receipt) => management.RegisterReceipt(in receipt);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ContainsReceipt(string transactionId) => management.ContainsReceipt(transactionId);

		#endregion


#if IAP_DEBUG
		public static IInAppPurchasingIntegration Integration => management.Integration;

		/// <returns>Предыдущий сервис</returns>
		public static IInAppPurchasingIntegration SetService(IInAppPurchasingIntegration integration) =>
			management.SetIntegration(integration);
#endif
	}
}
