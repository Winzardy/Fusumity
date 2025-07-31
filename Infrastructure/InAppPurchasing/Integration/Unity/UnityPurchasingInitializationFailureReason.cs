namespace InAppPurchasing.Unity
{
	public enum UnityPurchasingInitializationFailureReason
	{
		None,

		UnityServices,
		Disposed,

		PurchasingUnavailable,
		NoProductsAvailable,
		AppNotKnown,
		UnknownPlatform,
		UnknownCountry,

		Exception
	}
}
