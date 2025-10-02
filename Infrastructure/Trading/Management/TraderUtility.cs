using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Trading
{
	public static partial class TraderUtility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanExecute(this in TraderOfferReference reference, Tradeboard tradeboard, out TradeExecuteError? error)
		{
			tradeboard.FetchModeScope(true);
			return reference.Config
			   .trade
			   .CanFetch(tradeboard, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradeExecuteError?> ExecuteAsync(this in TraderOfferReference offerRef, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			return offerRef.Config.trade
			   .FetchAsync(tradeboard, cancellationToken);
		}
	}
}
