using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Content;

namespace Trading
{
	public static partial class TradeUtility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanPay(this in ContentReference<TradeCost> reference, Tradeboard tradeboard, out TradePayError? error)
		{
			tradeboard.Bind(in reference);
			return TradeManager.CanPay(reference, tradeboard, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradePayError?> PayAsync(this ContentReference<TradeCost> reference, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			tradeboard.Bind(in reference);
			return TradeManager.PayAsync(reference, tradeboard, cancellationToken);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanExecute(this in ContentReference<TradeEntry> reference, Tradeboard tradeboard, out TradeExecuteError? error)
		{
			tradeboard.Bind(in reference.Read());
			return TradeManager.CanExecute(reference, tradeboard, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradeExecuteError?> ExecuteAsync(this in ContentReference<TradeEntry> reference, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			tradeboard.Bind(in reference.Read());
			return TradeManager.ExecuteAsync(reference, tradeboard, cancellationToken);
		}
	}
}
