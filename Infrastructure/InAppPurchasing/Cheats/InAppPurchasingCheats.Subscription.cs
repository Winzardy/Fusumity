using MobileConsole;

namespace InAppPurchasing.Cheats.Subscription
{
	[ExecutableCommand(name = InAppPurchasingCheatsUtility.COMMAND_PATH + nameof(IAPProductType.Subscription) + "/Purchase..")]
	public class IAPPurchaseSubscriptionCheats : Command
	{
		[Dropdown(methodName: nameof(GetProducts))]
		public string product;

		public IAPPurchaseSubscriptionCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute() => InAppPurchasingCheatsUtility.RequestPurchase(IAPProductType.NonConsumable, product);

		private string[] GetProducts() => InAppPurchasingCheatsUtility.GetProducts<IAPSubscriptionProductEntry>();
	}

	[ExecutableCommand(name = InAppPurchasingCheatsUtility.COMMAND_PATH + nameof(IAPProductType.Subscription) + "/Can Purchase..")]
	public class IAPCanPurchaseSubscriptionCheats : Command
	{
		[Dropdown(methodName: nameof(GetProducts))]
		public string product;

		public IAPCanPurchaseSubscriptionCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;
		public override void Execute() => InAppPurchasingCheatsUtility.LogCanPurchase(IAPProductType.Subscription, product);

		private string[] GetProducts() => InAppPurchasingCheatsUtility.GetProducts<IAPSubscriptionProductEntry>();
	}

	[ExecutableCommand(name = InAppPurchasingCheatsUtility.COMMAND_PATH + nameof(IAPProductType.Subscription) + "/Get Info")]
	public class IAPGetInfoSubscriptionCheats : Command
	{
		[Dropdown(methodName: nameof(GetProducts))]
		public string product;

		public IAPGetInfoSubscriptionCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute()
		{
			if(product == InAppPurchasingCheatsUtility.NONE)
				return;

			ref readonly var subscriptionInfo = ref IAPManager.GetSubscriptionInfo(product);
			if (subscriptionInfo)
				IAPDebug.Log(subscriptionInfo);
			else
				IAPDebug.LogError($"Failed to get subscription info by product [ {product} ]");
		}

		private string[] GetProducts() => InAppPurchasingCheatsUtility.GetProducts<IAPSubscriptionProductEntry>();
	}
}
