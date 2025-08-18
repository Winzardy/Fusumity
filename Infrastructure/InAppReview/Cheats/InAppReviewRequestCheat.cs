using InAppReview;
using MobileConsole;
using Messaging;

namespace Game.Cheats.App
{
	[ExecutableCommand(name = "App/In App Review/Request")]
	public class InAppReviewRequestCheat : Command
	{
		public InAppReviewRequestCheat()
		{
			info.actionAfterExecuted = ActionAfterExecuted.CloseConsole;
		}

		public override void Execute() => new InAppReviewRequestMessage().Send();
	}
}
