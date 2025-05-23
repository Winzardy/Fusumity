using MobileConsole;

namespace Advertising.Cheats.Rewarded
{
	[ExecutableCommand(name = AdvertisingCheatsUtility.PATH + "Rewarded/Show")]
	public class ShowRewardedAdCheats : Command
	{
		[Dropdown(methodName: nameof(GetPlacements))]
		public string placement;

		public ShowRewardedAdCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute() => AdvertisingCheatsUtility.RequestShow(AdPlacementType.Rewarded, placement);

		private string[] GetPlacements() => AdvertisingCheatsUtility.GetPlacements<RewardedAdPlacementEntry>();
	}

	[ExecutableCommand(name = AdvertisingCheatsUtility.PATH + "Rewarded/Can Show")]
	public class CanShowRewardedAdCheats : Command
	{
		[Dropdown(methodName: nameof(GetPlacements))]
		public string placement;

		public CanShowRewardedAdCheats() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute() => AdvertisingCheatsUtility.LogCanShow(AdPlacementType.Rewarded, placement);

		private string[] GetPlacements() => AdvertisingCheatsUtility.GetPlacements<RewardedAdPlacementEntry>();
	}
}
