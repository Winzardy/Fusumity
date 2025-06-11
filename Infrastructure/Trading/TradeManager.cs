using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Content;
using Sapientia;

namespace Trading
{
	using TradeCostReference = ContentReference<TradeCost>;

	public interface ITradeManagement
	{
		public Task<bool> PrepayAsync(in TradeCostReference cost, Tradeboard tradeboard, out TradePayError? error);
		public bool CanPay(in TradeCostReference cost, Tradeboard tradeboard, out TradePayError? error);
		public Task<bool> ExecuteAsync(in TradeEntry trade, Tradeboard tradeboard, out TradeExecuteError? error);
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
		public static Task<bool> PrepayAsync(in TradeCostReference cost, Tradeboard tradeboard, out TradePayError? error) =>
			management.PrepayAsync(in cost, tradeboard, out error);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanPay(in TradeCostReference cost, Tradeboard tradeboard, out TradePayError? error) =>
			management.CanPay(in cost, tradeboard, out error);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<bool> ExecuteAsync(in TradeEntry trade, Tradeboard tradeboard, out TradeExecuteError? error)
			=> management.ExecuteAsync(in trade, tradeboard, out error);
	}

	public static class TradeManagerUtility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanPay(this in TradeCostReference cost, Tradeboard tradeboard)
			=> TradeManager.CanPay(cost, tradeboard, out _);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanPay(this in TradeCostReference cost, Tradeboard tradeboard, out TradePayError? error)
			=> TradeManager.CanPay(cost, tradeboard, out error);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<bool> PrepayAsync(this in TradeCostReference cost, Tradeboard tradeboard)
			=> TradeManager.PrepayAsync(cost, tradeboard, out _);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<bool> PrepayAsync(this in TradeCostReference cost, Tradeboard tradeboard, out TradePayError? error)
			=> TradeManager.PrepayAsync(cost, tradeboard, out error);
	}
}
