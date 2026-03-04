using Content;

namespace InAppPurchasing
{
	public static class IAPManagerUtility
	{
		public static ref readonly ProductInfo ToProductInfo<T>(this in ContentReference<T> entry, bool forceUpdateCache = false)
			where T : IAPProductEntry
			=> ref IAPManager.GetProductInfo(entry.Read(), forceUpdateCache);

		public static IAPProductEntry ToEntry(this in PurchaseReceipt receipt)
			=> IAPManager.GetEntry(receipt.productType, receipt.productId);
	}
}
