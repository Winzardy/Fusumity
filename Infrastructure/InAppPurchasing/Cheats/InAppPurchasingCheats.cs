using System;
using System.Collections.Generic;
using System.Linq;
using MobileConsole;
using Sapientia.Pooling;

namespace InAppPurchasing.Cheats
{
	[ExecutableCommand(name = InAppPurchasingCheatsUtility.COMMAND_PATH + "Product Status/Log Selected")]
	public class IAPPurchaseProductStatusLogSelectCheats : Command
	{
		private Dictionary<string, IAPProductEntry> _nameToEntry = new(2);

		[Dropdown(methodName: nameof(GetProducts))]
		public string product;

		public IAPPurchaseProductStatusLogSelectCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

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
				var id = $"[{entry.Type}] {entry.Id}";
				_nameToEntry[id] = entry;
			}

			return _nameToEntry.Select(x => x.Key).ToArray();
		}
	}

	[ExecutableCommand(name = InAppPurchasingCheatsUtility.COMMAND_PATH + "Product Status/Log All")]
	public class IAPPurchaseProductStatusLogAllCheats : Command
	{
		public IAPPurchaseProductStatusLogAllCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute()
		{
			using (StringBuilderPool.Get(out var sb))
			{
				foreach (var product in InAppPurchasingCheatsUtility.GetProducts())
				{
					var status = IAPManager.GetStatus(product);
					sb.Append($"[{product.Type}] Product by [ {product.Id} ] has status: {status}\n");
				}

				IAPDebug.Log(sb.ToString()
				   .Trim());
			}
		}
	}


}
