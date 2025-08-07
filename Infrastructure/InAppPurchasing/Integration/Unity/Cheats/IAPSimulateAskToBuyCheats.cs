#if UNITY_IOS
using InAppPurchasing.Cheats;
using MobileConsole;

namespace InAppPurchasing.Unity.Cheats
{
	[SettingCommand(name = InAppPurchasingCheatsUtility.SETTINGS_PATH)]
	public class IAPSimulateAskToBuyCheats : Command
	{
		[Variable(OnValueChanged = nameof(OnUseFakeUpdated))]
		public bool simulateAskToBuy;

		private IInAppPurchasingIntegration _main;

		public void OnUseFakeUpdated()
		{
			if (!IAPManager.IsInitialized)
				return;

			if (IAPManager.Integration is UnityPurchasingIntegration integration)
				integration.SetSimulateAskToBuy(simulateAskToBuy);
		}
	}
}
#endif
