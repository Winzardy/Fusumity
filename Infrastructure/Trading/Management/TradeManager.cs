using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Sapientia;

namespace Trading
{
	public interface ITradeManagement
	{
		public bool CanFetchOrExecute(TradeCost cost, Tradeboard tradeboard, out TradePayError? error);

		public Task<TradePayError?> FetchAsync(TradeCost cost, Tradeboard tradeboard, CancellationToken cancellationToken = default);

		public bool CanFetchOrExecute(in TradeConfig trade, Tradeboard tradeboard, out TradeExecuteError? error);

		public Task<TradeExecuteError?> FetchAsync(TradeConfig trade, Tradeboard tradeboard,
			CancellationToken cancellationToken = default);

		public void GetService(out ITradingService service);
	}

	// TODO: Убрать TradeManager. Нужную часть кода перенести в TradeUtility
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
		internal static bool CanFetchOrExecute(TradeCost cost, Tradeboard tradeboard, out TradePayError? error) =>
			management.CanFetchOrExecute(cost, tradeboard, out error);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Task<TradePayError?> FetchAsync(TradeCost cost, Tradeboard tradeboard,
			CancellationToken cancellationToken = default) =>
			management.FetchAsync(cost, tradeboard, cancellationToken);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool CanFetchOrExecute(in TradeConfig trade, Tradeboard tradeboard, out TradeExecuteError? error) =>
			management.CanFetchOrExecute(in trade, tradeboard, out error);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Task<TradeExecuteError?> FetchAsync(in TradeConfig trade, Tradeboard tradeboard,
			CancellationToken cancellationToken = default) =>
			management.FetchAsync(trade, tradeboard, cancellationToken);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GetService(out ITradingService service) => management.GetService(out service);
	}
}
