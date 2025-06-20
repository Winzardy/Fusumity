using System.Collections.Generic;
using System.Linq;
using MobileConsole;

namespace InAppPurchasing.Cheats
{
	[ExecutableCommand(name = InAppPurchasingCheatsUtility.COMMAND_PATH + "Get Product Status")]
	public class IAPPurchaseSubscriptionAdCheats : Command
	{
		private Dictionary<string, IAPProductEntry> _nameToEntry = new(2);

		[Dropdown(methodName: nameof(GetProducts))]
		public string product;

		public IAPPurchaseSubscriptionAdCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute()
		{
			if (!_nameToEntry.TryGetValue(product, out var entry))
			{
				IAPDebug.LogError("Not found product [ " + product + " ]");
				return;
			}

			var status = IAPManager.GetStatus(entry);
			IAPDebug.Log($"[{entry.Type}] Product by [ {entry.Id} ] has status: {status} ");
		}

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
