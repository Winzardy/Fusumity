using System;
using System.Collections.Generic;
using System.Linq;
using MobileConsole;

namespace InAppPurchasing.Cheats
{
	[ExecutableCommand(name = InAppPurchasingCheatsUtility.COMMAND_PATH + "Fake Restore/All")]
	public class IAPRestoreAllCheats : Command
	{
		public IAPRestoreAllCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute()
		{
			foreach (var productEntry in InAppPurchasingCheatsUtility.GetProducts())
			{
				var transactionId = Guid.NewGuid().ToString();
				IAPManager.GrantCenter.Grant(new PurchaseReceipt
				{
					productId = productEntry.Id,
					productType = productEntry.Type,

					transactionId = transactionId
				}, IAPRestoreSelectedCheats.OnGrant);
			}
		}
	}

	[ExecutableCommand(name = InAppPurchasingCheatsUtility.COMMAND_PATH + "Fake Restore/Selected")]
	public class IAPRestoreSelectedCheats : Command
	{
		private Dictionary<string, IAPProductEntry> _nameToEntry = new(2);

		[Dropdown(methodName: nameof(GetProducts))]
		public string product;

		public IAPRestoreSelectedCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute()
		{
			if (!_nameToEntry.TryGetValue(product, out var productEntry))
			{
				IAPDebug.LogError("Not found product [ " + product + " ]");
				return;
			}

			var transactionId = Guid.NewGuid().ToString();
			IAPManager.GrantCenter.Grant(new PurchaseReceipt
			{
				productId = productEntry.Id,
				productType = productEntry.Type,

				transactionId = transactionId
			}, OnGrant);
		}

		public static void OnGrant(in PurchaseReceipt receipt)
			=> IAPDebug.Log($"Fake restore for product (type: {receipt.productType}, id:{receipt.productId})");

		private string[] GetProducts()
		{
			_nameToEntry.Clear();

			foreach (var entry in InAppPurchasingCheatsUtility.GetProducts())
			{
				var id = $"[{entry.Type}] {entry}";
				_nameToEntry[id] = entry;
			}

			return _nameToEntry.Select(x => x.Key).ToArray();
		}
	}
}
