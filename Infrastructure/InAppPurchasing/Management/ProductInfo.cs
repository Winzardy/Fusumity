namespace InAppPurchasing
{
	public readonly struct ProductInfo
	{
		public readonly string id;
		public readonly IAPProductType type;
		public readonly string priceLabel;

		// Структуру можно дополнять, не является финальной
		public ProductInfo(string id, IAPProductType type, string priceLabel)
		{
			this.id = id;
			this.type = type;
			this.priceLabel = priceLabel;
		}

		public override string ToString()
		{
			return "SubscriptionInfo:\n" +
				$"  ID: {id}\n" +
				$"  Type: {type}\n" +
				$"  Price Label: {priceLabel}\n";
		}
	}
}
