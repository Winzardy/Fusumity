namespace InAppPurchasing.Unity
{
	public enum UnityPurchasingInitializationFailureReason
	{
		None,

		Canceled,

		UnityServices,

		PurchasingUnavailable,
		NoProductsAvailable,
		AppNotKnown,
		UnknownBilling,
		UnknownCountry,

		Exception,
	}
}
