using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Sapientia;

namespace Trading
{
	public interface ITradeManagement
	{
		public bool CanPay(TradeCost cost, Tradeboard tradeboard, out TradePayError? error);

		public Task<TradePayError?> PayAsync(TradeCost cost, Tradeboard tradeboard, CancellationToken cancellationToken = default);

		public bool CanExecute(in TradeEntry trade, Tradeboard tradeboard, out TradeExecuteError? error);

		public Task<TradeExecuteError?> ExecuteAsync(TradeEntry trade, Tradeboard tradeboard, CancellationToken cancellationToken = default);
	}

	public class TradeManager : StaticProvider<ITradeManagement>
	{
		private static ITradeManagement management
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		internal static bool IsInitialized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance != null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool CanPay(TradeCost cost, Tradeboard tradeboard, out TradePayError? error) =>
			management.CanPay(cost, tradeboard, out error);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Task<TradePayError?> PayAsync(TradeCost cost, Tradeboard tradeboard,
			CancellationToken cancellationToken = default) =>
			management.PayAsync(cost, tradeboard, cancellationToken);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool CanExecute(in TradeEntry trade, Tradeboard tradeboard, out TradeExecuteError? error) =>
			management.CanExecute(in trade, tradeboard, out error);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Task<TradeExecuteError?> ExecuteAsync(in TradeEntry trade, Tradeboard tradeboard,
			CancellationToken cancellationToken = default) =>
			management.ExecuteAsync(trade, tradeboard, cancellationToken);
	}
}
