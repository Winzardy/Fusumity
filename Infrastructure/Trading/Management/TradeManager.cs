using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Content;
using Sapientia;
using Sapientia.Pooling;

namespace Trading
{
	using TraderReference = ContentReference<TraderEntry>;
	using TradeReference = ContentReference<TradeEntry>;

	public interface ITradeManagement
	{
		public bool CanPay(TradeCost cost, Tradeboard tradeboard, out TradePayError? error);

		public Task<TradePayError?> PayAsync(TradeCost cost, Tradeboard tradeboard, CancellationToken cancellationToken = default);

		public bool CanExecute(in TradeEntry trade, Tradeboard tradeboard, out TradeExecuteError? error);

		public Task<TradeExecuteError?> ExecuteAsync(TradeEntry trade, Tradeboard tradeboard,
			CancellationToken cancellationToken = default);

		public PooledObject<Tradeboard>? CreateTradeboard(in TraderReference trader);
		public bool PushTrade(in TraderReference trader, in TradeReference trade);
	}

	/// <summary>
	/// Только на стороне клиента
	/// </summary>
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


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static PooledObject<Tradeboard>? CreateTradeboard(in TraderReference trader)
			=> management.CreateTradeboard(in trader);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool PushTrade(in TraderReference trader, in TradeReference trade)
			=> management.PushTrade(in trader, in trade);
	}
}
