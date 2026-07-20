using System.Threading;
using System.Threading.Tasks;
using Sapientia.Utility;

namespace Fusumity.Utility
{
	public class WaitFramesAsyncFlowController : AsyncFlowControllerBase
	{
		private readonly int _framesCount;

		public WaitFramesAsyncFlowController(int intervalMs, int framesCount) : base(intervalMs)
		{
			_framesCount = framesCount;
		}

		protected override async Task OnInterationAsync(CancellationToken token)
		{
			await Wait.NextFrames(_framesCount, token);
		}
	}
}
