using System;
using InAppPurchasing;

namespace Booting.InAppPurchasing
{
	public class InAppPurchasingEventsObserver : IDisposable
	{
		public InAppPurchasingEventsObserver()
		{
			IAPManager.Events.PurchaseCompleted += OnPurchaseCompleted;
			IAPManager.Events.PurchaseFailed += OnPurchaseFailed;
			IAPManager.Events.PurchaseRequested += OnPurchaseRequested;
			IAPManager.Events.PurchaseCanceled += OnPurchaseCanceled;
		}

		public void Dispose()
		{
			IAPManager.Events.PurchaseCompleted -= OnPurchaseCompleted;
			IAPManager.Events.PurchaseFailed -= OnPurchaseFailed;
			IAPManager.Events.PurchaseRequested -= OnPurchaseRequested;
			IAPManager.Events.PurchaseCanceled -= OnPurchaseCanceled;
		}

		private void OnPurchaseCompleted(in PurchaseReceipt receipt, object _) =>
			IAPDebug.Log($"[{receipt.productType}] [ {receipt.productId} ] purchased");

		private void OnPurchaseFailed(IAPProductEntry product, string error, object _) =>
			IAPDebug.LogError($"[{product.Type}] [ {product.Id} ] failed to purchase: {error}");

		private void OnPurchaseRequested(IAPProductEntry product) => IAPDebug.Log($"[{product.Type}] [ {product.Id} ] requested purchase");

		private void OnPurchaseCanceled(IAPProductEntry product, object _) =>
			IAPDebug.Log($"[{product.Type}] [ {product.Id} ] canceled purchase");
	}
}
