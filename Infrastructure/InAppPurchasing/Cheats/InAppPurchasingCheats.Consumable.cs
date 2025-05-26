using MobileConsole;

namespace InAppPurchasing.Cheats.Consumable
{
	[ExecutableCommand(name = InAppPurchasingCheatsUtility.COMMAND_PATH + nameof(IAPProductType.Consumable) + "/Purchase")]
	public class IAPPurchaseConsumableAdCheats : Command
	{
		[Dropdown(methodName: nameof(GetProducts))]
		public string product;

		public IAPPurchaseConsumableAdCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute() => InAppPurchasingCheatsUtility.RequestPurchase(IAPProductType.Consumable, product);

		private string[] GetProducts() => InAppPurchasingCheatsUtility.GetProducts<IAPConsumableProductEntry>();
	}

	[ExecutableCommand(name = InAppPurchasingCheatsUtility.COMMAND_PATH + nameof(IAPProductType.Consumable) + "/Can Purchase")]
	public class IAPCanPurchaseConsumableAdCheats : Command
	{
		[Dropdown(methodName: nameof(GetPlacements))]
		public string product;

		public IAPCanPurchaseConsumableAdCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute() => InAppPurchasingCheatsUtility.LogCanPurchase(IAPProductType.Consumable, product);

		private string[] GetPlacements() => InAppPurchasingCheatsUtility.GetProducts<IAPConsumableProductEntry>();
	}
}
