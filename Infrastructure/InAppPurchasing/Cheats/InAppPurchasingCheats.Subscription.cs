using MobileConsole;

namespace InAppPurchasing.Cheats.Subscription
{
	[ExecutableCommand(name = InAppPurchasingCheatsUtility.PATH + nameof(IAPProductType.Subscription) + "/Purchase..")]
	public class IAPPurchaseSubscriptionAdCheats : Command
	{
		[Dropdown(methodName: nameof(GetProducts))]
		public string product;

		public IAPPurchaseSubscriptionAdCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute() => InAppPurchasingCheatsUtility.RequestPurchase(IAPProductType.NonConsumable, product);

		private string[] GetProducts() => InAppPurchasingCheatsUtility.GetProducts<IAPSubscriptionProductEntry>();
	}

	[ExecutableCommand(name = InAppPurchasingCheatsUtility.PATH + nameof(IAPProductType.Subscription) + "/Can Purchase..")]
	public class IAPCanPurchaseSubscriptionAdCheats : Command
	{
		[Dropdown(methodName: nameof(GetProducts))]
		public string product;

		public IAPCanPurchaseSubscriptionAdCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;
		public override void Execute() => InAppPurchasingCheatsUtility.LogCanPurchase(IAPProductType.Subscription, product);

		private string[] GetProducts() => InAppPurchasingCheatsUtility.GetProducts<IAPSubscriptionProductEntry>();
	}

	[ExecutableCommand(name = InAppPurchasingCheatsUtility.PATH + nameof(IAPProductType.Subscription) + "/Get Info")]
	public class IAPGetInfoSubscriptionAdCheats : Command
	{
		[Dropdown(methodName: nameof(GetProducts))]
		public string product;

		public IAPGetInfoSubscriptionAdCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

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
