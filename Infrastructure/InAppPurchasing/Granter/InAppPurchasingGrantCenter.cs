using System;
using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Reflection;

namespace InAppPurchasing
{
	public class InAppPurchasingGrantCenter : IInAppPurchasingGrantCenter
	{
		private List<IIAPPurchaseGranter> _registeredGranters;

		private Queue<Pair> _queue;

		private bool _initialized;

		public void Initialize()
		{
			_initialized = true;

			foreach (var granter in _registeredGranters)
				granter.Initialize();

			if (_queue.IsNullOrEmpty())
				return;

			while (_queue.TryDequeue(out var pair))
				Grant(in pair.receipt, pair.callback);
		}

		public IIAPPurchaseGranter CreateOrRegister(Type type)
		{
			var granter = type.CreateInstance<IIAPPurchaseGranter>();
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
