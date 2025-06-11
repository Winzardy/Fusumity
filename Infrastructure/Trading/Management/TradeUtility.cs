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

		// [MethodImpl(MethodImplOptions.AggressiveInlining)]
		// public static bool CanPay(this in TradeEntry trade, Tradeboard tradeboard, out TradePayError? error)
		// {
		// 	tradeboard.Bind(in trade);
		// 	return TradeManager.CanPay(trade.cost, tradeboard, out error);
		// }
		//
		// [MethodImpl(MethodImplOptions.AggressiveInlining)]
		// public static Task<TradePayError?> PayAsync(this in TradeEntry trade, Tradeboard tradeboard,
		// 	CancellationToken cancellationToken = default)
		// {
		// 	tradeboard.Bind(in trade);
		// 	return TradeManager.PayAsync(trade.cost, tradeboard, cancellationToken);
		// }



		// public static bool CanExecute(this in TradeEntry trade, Tradeboard board, out TradeExecuteError? error)
		// {
		// 	var result = true;
		// 	error = null;
		//
		// 	if (!trade.cost.CanExecute(board, out var payError))
		// 		result = false;
		//
		// 	if (!trade.reward.CanReceive(board, out var receiveError))
		// 		result = false;
		//
		// 	if (!result)
		// 		error = new TradeExecuteError(payError, receiveError);
		//
		// 	return result;
		// }
		//
		// internal static async Task<bool> ExecuteAsync(this TradeEntry trade, Tradeboard board, CancellationToken cancellationToken)
		// {
		// 	// Сначала платим
		// 	var success = await trade.cost.ExecuteAsync(board, cancellationToken);
		// 	if (!success)
		// 		return false;
		//
		// 	// Потом получаем
		// 	success = await trade.reward.ExecuteAsync(board, cancellationToken);
		//
		// 	// Если что-то пошло не по плану возвращаем
		// 	if (!success)
		// 		await trade.cost.ExecuteRefundAsync(board, cancellationToken);
		//
		// 	return success;
		// }
	}

	public struct TradeExecuteError
	{
		public TradePayError? payError;
		public TradeReceiveError? receiveError;

		public TradeExecuteError(TradePayError? payError, TradeReceiveError? receiveError)
		{
			this.payError = payError;
			this.receiveError = receiveError;
		}
	}
}
