using System;
using System.Collections.Generic;
using Fusumity.Reactive;
using Fusumity.Utility;
using Sirenix.OdinInspector;

namespace InAppPurchasing.Offline
{
	[Serializable]
	[TypeRegistryItem("\u2009Offline", "", SdfIconType.Octagon)]
	public class OfflineInAppPurchasingServiceFactory : IInAppPurchasingServiceFactory
	{
		public IInAppPurchasingService Create() => new OfflineInAppPurchasingService();
	}

	public class OfflineInAppPurchasingService : IInAppPurchasingService, IDisposable
	{
		private const string ALL_SAVE_KEY = "iap_all_transactions";

		private readonly List<string> _transactionsToSave = new();

		public void Initialize()
		{
			UnityLifecycle.LateUpdateEvent.Subscribe(OnLateUpdate);
		}

		public void Dispose()
		{
			UnityLifecycle.LateUpdateEvent.UnSubscribe(OnLateUpdate);
		}

		public bool Contains(string transactionId)
			=> LocalSave.Has(transactionId);

		public InAppPurchasingRegisterResult Register(string transactionId, PurchaseReceipt receipt)
		{
			_transactionsToSave.Add(transactionId);
			LocalSave.Save(transactionId, receipt);

			return InAppPurchasingRegisterResult.Done;
		}

		public PurchaseReceipt? GetReceipt(string transactionId)
		{
			if (LocalSave.Has(transactionId))
				return LocalSave.Load<PurchaseReceipt>(transactionId);

			return null;
		}

		public string[] GetAllTransactions() => LocalSave.Load(ALL_SAVE_KEY, new List<string>(0))
		   .ToArray();

		public DateTime GetUtcNow() => DateTime.UtcNow;

		private void OnLateUpdate()
		{
			if (_transactionsToSave.Count <= 0)
				return;

			var all = LocalSave.Load(ALL_SAVE_KEY, new List<string>(2));
			all.AddRange(_transactionsToSave);
			_transactionsToSave.Clear();
			LocalSave.Save(ALL_SAVE_KEY, all);
		}
	}
}
