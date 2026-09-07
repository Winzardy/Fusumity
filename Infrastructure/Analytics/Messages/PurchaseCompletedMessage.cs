namespace Analytics
{
	/// <summary>
	/// Успешная покупка за реальные деньги
	/// </summary>
	/// <remarks>
	/// Про выручку должны узнать все интеграции, но собрать её можно только рядом с биллингом,
	/// поэтому покупка доезжает до интеграций сообщением
	/// </remarks>
	public struct PurchaseCompletedMessage
	{
		public string productId;
		public string transactionId;

		/// <summary>
		/// Токен Google Play, по нему MMP сам сверяет чек и достаёт сумму
		/// </summary>
		public string purchaseToken;

		public decimal price;

		/// <summary>
		/// Код валюты по ISO 4217
		/// </summary>
		public string currency;

		public bool subscription;
	}
}
