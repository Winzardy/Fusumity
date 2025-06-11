using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Content;
using Sapientia;

namespace Trading
{
	using TradeCostReference = ContentReference<TradeCost>;

	public interface ITradeManagement
	{
		// public bool CanPrepay(in TradeCostReference reference, Tradeboard tradeboard, out TradePayError? error);
		//
		// public Task<TradePayError?> PrepayAsync(TradeCostReference reference, Tradeboard tradeboard,
		// 	CancellationToken cancellationToken = default);
		public bool CanPrepay(TradeCost cost, Tradeboard tradeboard, out TradePayError? error);

		public Task<TradePayError?> PrepayAsync(TradeCost cost, Tradeboard tradeboard, CancellationToken cancellationToken = default);
	}

	public class TradeManager : StaticProvider<ITradeManagement>
	{
		private static ITradeManagement management
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		public static bool IsInitialized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance != null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanPay(TradeCost cost, Tradeboard tradeboard, out TradePayError? error) =>
			management.CanPrepay(cost, tradeboard, out error);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradePayError?> PrepayAsync(TradeCost cost, Tradeboard tradeboard,
			CancellationToken cancellationToken = default) =>
			management.PrepayAsync(cost, tradeboard, cancellationToken);
	}

	public static class TradeManagerUtility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanPay(in TradeCostReference reference, Tradeboard tradeboard, out TradePayError? error)
		{
			tradeboard.Bind(in reference);
			return TradeManager.CanPay(reference, tradeboard, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradePayError?> PrepayAsync(TradeCostReference reference, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			tradeboard.Bind(in reference);
			return TradeManager.PrepayAsync(reference, tradeboard, cancellationToken);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanPay(this in TradeEntry trade, Tradeboard tradeboard, out TradePayError? error)
		{
			tradeboard.Bind(in trade);
			return TradeManager.CanPay(trade.cost, tradeboard, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradePayError?> PrepayAsync(in TradeEntry trade, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			tradeboard.Bind(in trade);
			return TradeManager.PrepayAsync(trade.cost, tradeboard, cancellationToken);
		}
	}
}
