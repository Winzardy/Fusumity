using Sapientia.Extensions;

namespace InAppPurchasing
{
	/// <summary>
	/// Гарантирует выдачу покупки на основе <see cref="PurchaseReceipt"/>.
	/// Используется в ситуациях, когда покупка по каким-то причинам не "обработалась" сразу — например,
	/// в случае восстановления, отложенной транзакции или повторной обработки после сбоя
	/// </summary>
	public interface IPurchaseGranter
	{
		public void Initialize();

		bool Grant(in PurchaseReceipt receipt);
	}

	/// <inheritdoc cref="IPurchaseGranter"/>
	public abstract class PurchaseGranter : CompositeDisposable, IPurchaseGranter
	{
		public void Initialize()
		{
			OnInitializeInternal();
		}

		protected virtual void OnInitializeInternal() => OnInitialize();

		protected virtual void OnInitialize()
		{
		}

		public abstract bool Grant(in PurchaseReceipt receipt);
	}
}
