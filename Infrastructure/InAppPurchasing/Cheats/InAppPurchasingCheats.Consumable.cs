using MobileConsole;

namespace InAppPurchasing.Cheats.Consumable
{
	[ExecutableCommand(name = InAppPurchasingCheatsUtility.COMMAND_PATH + nameof(IAPProductType.Consumable) + "/Purchase")]
	public class IAPPurchaseConsumableCheats : Command
	{
		[Dropdown(methodName: nameof(GetProducts))]
		public string product;

		public IAPPurchaseConsumableCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute() => InAppPurchasingCheatsUtility.RequestPurchase(IAPProductType.Consumable, product);

		private string[] GetProducts() => InAppPurchasingCheatsUtility.GetProducts<IAPConsumableProductEntry>();
	}

	[ExecutableCommand(name = InAppPurchasingCheatsUtility.COMMAND_PATH + nameof(IAPProductType.Consumable) + "/Can Purchase")]
	public class IAPCanPurchaseConsumableCheats : Command
	{
		[Dropdown(methodName: nameof(GetPlacements))]
		public string product;

		public IAPCanPurchaseConsumableCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute() => InAppPurchasingCheatsUtility.LogCanPurchase(IAPProductType.Consumable, product);

		private string[] GetPlacements() => InAppPurchasingCheatsUtility.GetProducts<IAPConsumableProductEntry>();
	}
}
