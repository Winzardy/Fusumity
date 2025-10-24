using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Content;

namespace Trading
{
	public static partial class TradeUtility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanFetch(this ContentEntry<TradeCost> costEntry, Tradeboard tradeboard, out TradePayError? error)
			=> CanFetch(costEntry.ToReference(), tradeboard, out error);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanFetch(this in ContentReference<TradeCost> costRef, Tradeboard tradeboard, out TradePayError? error)
		{
			tradeboard.Bind(in costRef);
			return TradeManager.CanFetchOrExecute(costRef, tradeboard, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanFetch(this TradeCost cost, Tradeboard tradeboard, string tradeId, out TradePayError? error)
		{
			tradeboard.Bind(tradeId);
			return TradeManager.CanFetchOrExecute(cost, tradeboard, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradePayError?> FetchAsync(this ContentEntry<TradeCost> costEntry, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			return FetchAsync(costEntry.ToReference(), tradeboard, cancellationToken);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradePayError?> FetchAsync(this in ContentReference<TradeCost> costRef, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			tradeboard.Bind(in costRef);
			return TradeManager.FetchAsync(costRef, tradeboard, cancellationToken);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanFetch(this in ContentReference<TradeConfig> tradeRef, Tradeboard tradeboard, out TradeExecuteError? error)
		{
			tradeboard.Bind(in tradeRef.Read());
			return TradeManager.CanFetchOrExecute(tradeRef, tradeboard, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradeExecuteError?> FetchAsync(this in ContentReference<TradeConfig> tradeRef, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			tradeboard.Bind(in tradeRef.Read());
			return TradeManager.FetchAsync(tradeRef, tradeboard, cancellationToken);
		}
	}
}
