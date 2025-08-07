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
			return reference.GetEntry()
			   .trade
			   .CanExecute(tradeboard, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradeExecuteError?> ExecuteAsync(this in TraderOfferReference reference, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			return reference.GetEntry()
			   .trade
			   .ExecuteAsync(tradeboard, cancellationToken);
		}
	}
}
