namespace InAppPurchasing
{
	/// <summary>
	/// Принимает на себя рецепты от покупок типа deferred, восстановление или обработка ошибочных
	/// </summary>
	public interface IInAppPurchasingGrantCenter
	{
		/// <summary>
		/// Механизм при котором мы включаем/выключаем восстановление покупок
		/// </summary>
		void SetActive(bool active);
		bool Register<T>(T granter) where T : IIAPPurchaseGranter;
		bool Unregister<T>(T granter) where T : IIAPPurchaseGranter;

		void Grant(in PurchaseReceipt receipt, IntegrationCallback callback = null);
	}

	public delegate void IntegrationCallback(in PurchaseReceipt receipt);
}
