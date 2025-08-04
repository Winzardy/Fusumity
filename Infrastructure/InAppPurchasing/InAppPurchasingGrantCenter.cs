using System;
using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Reflection;

namespace InAppPurchasing
{
	/// <summary>
	/// Принимает на себя рецепты от покупок типа deferred, восстановление или обработка ошибочных
	/// </summary>
	public interface IInAppPurchasingGrantCenter
	{
		public void Initialize();
		public IPurchaseGranter CreateOrRegister(Type type);
		public void Grant(in PurchaseReceipt receipt, IntegrationCallback callback = null);
	}

	public delegate void IntegrationCallback(in PurchaseReceipt receipt);

	public class InAppPurchasingGrantCenter : IInAppPurchasingGrantCenter
	{
		private List<IPurchaseGranter> _registeredGranters;

		private Queue<Pair> _queue;

		private bool _initialized;

		public void Initialize()
		{
			_initialized = true;

			if (_queue.IsNullOrEmpty())
				return;

			while (_queue.TryDequeue(out var pair))
				Grant(in pair.receipt, pair.callback);
		}

		public IPurchaseGranter CreateOrRegister(Type type)
		{
			var granter = type.CreateInstance<IPurchaseGranter>();
			_registeredGranters ??= new();
			_registeredGranters.Add(granter);
			return granter;
		}

		public void Grant(in PurchaseReceipt receipt, IntegrationCallback callback = null)
		{
			if (!_initialized)
			{
				_queue ??= new();
				_queue.Enqueue(new Pair(receipt, callback));
				return;
			}

			foreach (var granter in _registeredGranters)
			{
				if (!granter.Grant(in receipt))
					continue;

				callback?.Invoke(in receipt);
				return;
			}

			// Можно наверно сохранить куда-то что для данного рецепта не выдали награду
			IAPDebug.LogError($"No granter handled receipt: transaction {receipt.transactionId} " +
				$"for product {receipt.productId} (type: {receipt.productType})");
		}

		private class Pair
		{
			public PurchaseReceipt receipt;
			public IntegrationCallback callback;

			public Pair(in PurchaseReceipt receipt, IntegrationCallback callback)
			{
				this.callback = callback;
				this.receipt = receipt;
			}
		}
	}
}
