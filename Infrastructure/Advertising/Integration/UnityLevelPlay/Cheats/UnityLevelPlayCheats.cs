using Fusumity.Utility;
using MobileConsole;

namespace Advertising.Cheats.UnityLevelPlay
{
	[ExecutableCommand(name = Constants.PATH + "Validate Integration")]
	public class UnityLevelPlayValidateIntegrationCheats : Command
	{
		public UnityLevelPlayValidateIntegrationCheats()
		{
			info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;
		}

		public override void Execute() => IronSource.Agent.validateIntegration();
	}

	[ExecutableCommand(name = Constants.PATH + "Log Advertiser Id")]
	public class UnityLevelPlayLogAdvertiserIdCheats : Command
	{
		public bool copyToClipboard = true;

		public UnityLevelPlayLogAdvertiserIdCheats()
		{
			info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;
		}

		public override void Execute()
		{
			var advertiserId = IronSource.Agent.getAdvertiserId();
			AdsDebug.Log($"Unity LevelPlay Advertiser Id: {advertiserId.UnderlineText().BoldText()}");
			if (copyToClipboard)
				advertiserId.CopyToClipboard();
		}
	}

	[ExecutableCommand(name = Constants.PATH + "Launch Test Suite")]
	public class UnityLevelPlayLaunchTestSuiteCheats : Command
	{
		public UnityLevelPlayLaunchTestSuiteCheats()
		{
			info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;
		}

		public override void Execute()
		{
			IronSource.Agent.launchTestSuite();
			AdsDebug.Log($"Unity LevelPlay requested launch test suite (only test device)");
		}
	}

	internal class Constants
	{
		internal const string PATH = "App/Advertising/Unity LevelPlay/";
	}
}
