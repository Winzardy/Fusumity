using Sapientia.Extensions;

namespace InAppPurchasing
{
	/// <summary>
	/// Гарантирует выдачу покупки на основе <see cref="PurchaseReceipt"/>.
	/// Используется в ситуациях, когда покупка по каким-то причинам не "обработалась" сразу — например,
	/// в случае восстановления, отложенной транзакции или повторной обработки после сбоя
	/// </summary>
	public interface IIAPPurchaseGranter
	{
		bool Grant(in PurchaseReceipt receipt);
	}

	/// <inheritdoc cref="IIAPPurchaseGranter"/>
	public abstract class IAPPurchaseGranter : CompositeDisposable, IIAPPurchaseGranter
	{
		public IAPPurchaseGranter()
		{
			if (!IAPManager.RegisterGranter(this))
				return;

			OnInitializeInternal();
		}

		protected override void OnDisposeInternal()
		{
			if (!IAPManager.UnregisterGranter(this))
				return;

			base.OnDisposeInternal();
		}

		protected virtual void OnInitializeInternal() => OnInitialize();

		protected virtual void OnInitialize()
		{
		}

		public abstract bool Grant(in PurchaseReceipt receipt);
	}
}
