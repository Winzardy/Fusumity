using MobileConsole;

namespace Advertising.Cheats.Interstitial
{
	[ExecutableCommand(name = AdvertisingCheatsUtility.PATH + "Interstitial/Show.")]
	public class ShowInterstitialAdCheats : Command
	{
		[Dropdown(methodName: nameof(GetPlacements))]
		public string placement;

		public ShowInterstitialAdCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute() => AdvertisingCheatsUtility.RequestShow(AdPlacementType.Interstitial, placement);

		private string[] GetPlacements() => AdvertisingCheatsUtility.GetPlacements<InterstitialAdPlacementEntry>();
	}

	[ExecutableCommand(name = AdvertisingCheatsUtility.PATH + "Interstitial/Can Show.")]
	public class CanShowInterstitialAdCheats : Command
	{
		[Dropdown(methodName: nameof(GetPlacements))]
		public string placement;

		public CanShowInterstitialAdCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;
		public override void Execute() => AdvertisingCheatsUtility.LogCanShow(AdPlacementType.Interstitial, placement);

		private string[] GetPlacements() => AdvertisingCheatsUtility.GetPlacements<InterstitialAdPlacementEntry>();
	}
}
