using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Content;

namespace Trading
{
	public static partial class TradeUtility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanFetch(this ContentEntry<TradeCost> entry, Tradeboard tradeboard, out TradePayError? error)
			=> CanFetch(entry.ToReference(), tradeboard, out error);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanFetch(this in ContentReference<TradeCost> reference, Tradeboard tradeboard, out TradePayError? error)
		{
			tradeboard.Bind(in reference);
			return TradeManager.CanFetchOrExecute(reference, tradeboard, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanFetch(this TradeCost cost, Tradeboard tradeboard, string tradeId, out TradePayError? error)
		{
			tradeboard.Bind(tradeId);
			return TradeManager.CanFetchOrExecute(cost, tradeboard, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradePayError?> FetchAsync(this ContentEntry<TradeCost> entry, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			return FetchAsync(entry.ToReference(), tradeboard, cancellationToken);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradePayError?> FetchAsync(this in ContentReference<TradeCost> reference, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			tradeboard.Bind(in reference);
			return TradeManager.FetchAsync(reference, tradeboard, cancellationToken);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanFetch(this in ContentReference<TradeConfig> reference, Tradeboard tradeboard, out TradeExecuteError? error)
		{
			tradeboard.Bind(in reference.Read());
			return TradeManager.CanFetchOrExecute(reference, tradeboard, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradeExecuteError?> FetchAsync(this in ContentReference<TradeConfig> reference, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			tradeboard.Bind(in reference.Read());
			return TradeManager.FetchAsync(reference, tradeboard, cancellationToken);
		}
	}
}
