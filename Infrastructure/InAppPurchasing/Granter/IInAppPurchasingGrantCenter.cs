using System;

namespace InAppPurchasing
{
	/// <summary>
	/// Принимает на себя рецепты от покупок типа deferred, восстановление или обработка ошибочных
	/// </summary>
	public interface IInAppPurchasingGrantCenter
	{
		public void Initialize();
		public IIAPPurchaseGranter CreateOrRegister(Type type);
		public void Grant(in PurchaseReceipt receipt, IntegrationCallback callback = null);
	}

	public delegate void IntegrationCallback(in PurchaseReceipt receipt);
}
