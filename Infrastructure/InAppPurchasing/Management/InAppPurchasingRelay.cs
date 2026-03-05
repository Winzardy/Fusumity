using Sapientia;

namespace InAppPurchasing
{
	internal class InAppPurchasingRelay : Relay<IInAppPurchasingIntegration>, IInAppPurchasingEvents
	{
		public event PurchaseCompleted PurchaseCompleted;
		public event PurchaseFailed PurchaseFailed;
		public event PurchaseRequested PurchaseRequested;
		public event PurchaseCanceled PurchaseCanceled;
		public event PurchaseDeferred PurchaseDeferred;
		public event PromotionalPurchaseIntercepted PromotionalPurchaseIntercepted;

		protected override void OnBind(IInAppPurchasingIntegration integration)
		{
			integration.PurchaseCompleted += OnPurchaseCompleted;
			integration.PurchaseDeferred += OnPurchaseDeferred;

			integration.PurchaseFailed += OnPurchaseFailed;
			integration.PurchaseRequested += OnPurchaseRequested;
			integration.PurchaseCanceled += OnPurchaseCanceled;

			integration.PromotionalPurchaseIntercepted += OnPromotionalPurchaseIntercepted;
		}

		protected override void OnClear(IInAppPurchasingIntegration integration)
		{
			integration.PurchaseCompleted -= OnPurchaseCompleted;
			integration.PurchaseDeferred -= OnPurchaseDeferred;

			integration.PurchaseFailed -= OnPurchaseFailed;
			integration.PurchaseRequested -= OnPurchaseRequested;
			integration.PurchaseCanceled -= OnPurchaseCanceled;

			integration.PromotionalPurchaseIntercepted -= OnPromotionalPurchaseIntercepted;
		}

		private void OnPurchaseCompleted(in PurchaseReceipt receipt, bool live, object rawData)
		{
			IAPDebug.Log($"[{receipt.productType}] [ {receipt.productId} ] purchased (live: {live})");
			PurchaseCompleted?.Invoke(in receipt, live, rawData);
		}

		private void OnPurchaseDeferred(IAPProductEntry product, object rawData)
		{
			IAPDebug.Log($"[{product.Type}] [ {product.Id} ] purchase deferred");
			PurchaseDeferred?.Invoke(product, rawData);
		}

		private void OnPurchaseFailed(IAPProductEntry product, string error, object rawData)
		{
			IAPDebug.LogError($"[{product.Type}] [ {product.Id} ] failed to purchase: {error}");
			PurchaseFailed?.Invoke(product, error, rawData);
		}

		private void OnPurchaseRequested(IAPProductEntry product)
		{
			IAPDebug.Log($"[{product.Type}] [ {product.Id} ] requested purchase");
			PurchaseRequested?.Invoke(product);
		}

		private void OnPurchaseCanceled(IAPProductEntry product, object rawData)
		{
			IAPDebug.Log($"[{product.Type}] [ {product.Id} ] canceled purchase");
			PurchaseCanceled?.Invoke(product, rawData);
		}

		private void OnPromotionalPurchaseIntercepted(IAPProductEntry product, object rawData)
		{
			IAPDebug.Log($"[{product.Type}] [ {product.Id} ] promotional purchase intercepted");
			PromotionalPurchaseIntercepted?.Invoke(product, rawData);
		}
	}
}
