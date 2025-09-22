using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

		private bool _saving;
		private readonly HashSet<string> _transactionsToSave = new();

		public DateTime DateTime => DateTime.UtcNow;

		public void Initialize()
		{
			UnityLifecycle.ApplicationShutdown += SaveTransactions;
		}

		public void Dispose()
		{
			UnityLifecycle.ApplicationShutdown -= SaveTransactions;
		}

		public bool Contains(string transactionId)
			=> LocalSave.Has(transactionId);

		public InAppPurchasingRegisterResult Register(in PurchaseReceipt receipt)
		{
			if (!_transactionsToSave.Add(receipt.transactionId))
			{
				IAPDebug.LogWarning($"Already registered transaction by id [ {receipt.transactionId} ]");
				return InAppPurchasingRegisterResult.Done;
			}

			LocalSave.Save(receipt.transactionId, receipt);

			if (!_saving)
			{
				_saving = true;
				SaveTransactionAsync().Forget();
			}

			return InAppPurchasingRegisterResult.Done;
		}

		public PurchaseReceipt? GetReceipt(string transactionId)
		{
			if (LocalSave.Has(transactionId))
				return LocalSave.Load<PurchaseReceipt>(transactionId);

			return null;
		}

		public IEnumerable<string> GetAllTransactions() => LocalSave.Load(ALL_SAVE_KEY, new List<string>(0))
		   .ToArray();

		private async UniTaskVoid SaveTransactionAsync()
		{
			await UniTask.DelayFrame(2);
			SaveTransactions();
		}

		private void SaveTransactions()
		{
			if (_transactionsToSave.Count > 0)
			{
				var all = LocalSave.Load(ALL_SAVE_KEY, new List<string>(2));
				all.AddRange(_transactionsToSave);
				_transactionsToSave.Clear();
				LocalSave.Save(ALL_SAVE_KEY, all);
			}

			_saving = false;
		}
	}
}
