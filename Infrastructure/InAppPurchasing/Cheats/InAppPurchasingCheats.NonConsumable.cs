using MobileConsole;

namespace InAppPurchasing.Cheats.NonConsumable
{
	[ExecutableCommand(name = InAppPurchasingCheatsUtility.COMMAND_PATH + nameof(IAPProductType.NonConsumable) + "/Purchase.")]
	public class IAPPurchaseNonConsumableCheats : Command
	{
		[Dropdown(methodName: nameof(GetProducts))]
		public string product;

		public IAPPurchaseNonConsumableCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute() => InAppPurchasingCheatsUtility.RequestPurchase(IAPProductType.NonConsumable, product);

		private string[] GetProducts() => InAppPurchasingCheatsUtility.GetProducts<IAPNonConsumableProductEntry>();
	}

	[ExecutableCommand(name = InAppPurchasingCheatsUtility.COMMAND_PATH + nameof(IAPProductType.NonConsumable) + "/Can Purchase.")]
	public class IAPCanPurchaseNonConsumableCheats : Command
	{
		[Dropdown(methodName: nameof(GetProducts))]
		public string product;

		public IAPCanPurchaseNonConsumableCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;
		public override void Execute() => InAppPurchasingCheatsUtility.LogCanPurchase(IAPProductType.NonConsumable, product);

		private string[] GetProducts() => InAppPurchasingCheatsUtility.GetProducts<IAPNonConsumableProductEntry>();
	}
}
