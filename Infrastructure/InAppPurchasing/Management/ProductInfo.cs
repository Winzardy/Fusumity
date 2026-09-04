namespace InAppPurchasing
{
	public readonly struct ProductInfo
	{
		public readonly string id;
		public readonly IAPProductType type;
		public readonly string priceLabel;

		/// <summary>
		/// Цена в валюте магазина, <c>0</c> если платформа её не отдала
		/// </summary>
		public readonly decimal price;

		/// <summary>
		/// Код валюты по ISO 4217
		/// </summary>
		public readonly string isoCurrencyCode;

		// Структуру можно дополнять, не является финальной
		public ProductInfo(string id, IAPProductType type, string priceLabel, decimal price = 0, string isoCurrencyCode = null)
		{
			this.id = id;
			this.type = type;
			this.priceLabel = priceLabel;
			this.price = price;
			this.isoCurrencyCode = isoCurrencyCode;
		}

		public override string ToString()
		{
			return "SubscriptionInfo:\n" +
				$"  ID: {id}\n" +
				$"  Type: {type}\n" +
				$"  Price Label: {priceLabel}\n" +
				$"  Price: {price} {isoCurrencyCode}\n";
		}
	}
}
