using System;
using System.Collections.Generic;

namespace InAppPurchasing
{
	/// <summary>
	/// Проектно-ориентированный интерфейс, адаптирующий <see cref="InAppPurchasing"/> под инфраструктуру и ограничения самого продукта (проекта)
	/// </summary>
	public interface IInAppPurchasingService
	{
		DateTime DateTime { get; }
		public bool Contains(string transactionId);
		public InAppPurchasingRegisterResult Register(in PurchaseReceipt receipt);
		PurchaseReceipt? GetReceipt(string transactionId);

		public IEnumerable<string> GetAllTransactions();
	}

	public enum InAppPurchasingRegisterResult
	{
		Done,
		Pending, // Interop
		// Failed
	}
}
