namespace InAppPurchasing.Unity
{
	public enum UnityPurchasingInitializationFailureReason
	{
		None,

		Canceled,

		UnityServices,

		/// <summary>
		/// Не удалось подключиться к магазину
		/// </summary>
		PurchasingUnavailable,

		/// <summary>
		/// Не удалось получить продукты из магазина
		/// </summary>
		NoProductsAvailable,
		UnknownBilling,
		UnknownCountry,

		Exception,
	}
}
