using System;
using System.Threading;
using System.Threading.Tasks;
using Content;
using JetBrains.Annotations;
using Sapientia;
using Sapientia.Extensions;
using Sapientia.Pooling;

namespace Trading
{
	public static partial class TradeUtility
	{
		private static bool CanFetchInternal(in TradeConfig trade, Tradeboard tradeboard, out TradeExecuteError? error)
		{
			if (!CanFetchInternal(trade.cost, tradeboard, out var payError))
			{
				error = new TradeExecuteError(payError, null);
				return false;
			}

			if (!trade.reward.CanExecute(tradeboard, out var receiveError))
			{
				error = new TradeExecuteError(payError, receiveError);
				return false;
			}

			error = null;
			return true;
		}

		private static bool CanFetchInternal([CanBeNull] TradeCost cost, Tradeboard tradeboard, out TradePayError? error)
		{
			if (!tradeboard.IsFetchMode)
				throw new ArgumentException("Tradeboard will be fetched!");

			if (cost == null)
			{
				error = null;
				return false;
			}

			foreach (var t in cost.EnumerateActual(tradeboard))
			{
				if (t is IInterceptableTradeCost interceptable)
				{
					if (interceptable.ShouldIntercept(tradeboard))
					{
						error = null;
						return true;
					}
				}

				if (t is ITradeCostWithReceipt tradeCostWithReceipt)
				{
					if (!tradeCostWithReceipt.CanFetch(tradeboard, out error))
						return false;
				}
				else
				{
					if (!t.CanExecute(tradeboard, out error))
						return false;
				}
			}

			error = null;
			return true;
		}

		private static async Task<TradeExecuteError?> FetchInternalAsync(TradeConfig trade, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			using (tradeboard.FetchModeScope())
			{
				if (!CanFetchInternal(in trade, tradeboard, out var error))
					return error;
			}

			var payError = await FetchInternalAsync(trade.cost, tradeboard, cancellationToken);
			if (payError != null)
				return new TradeExecuteError(payError, null);

			return null;
		}

		/// <param name="fullPay">Нужно ли в конце выполнить Pay если ITradingBackend не задан</param>
		private static async Task<TradePayError?> FetchInternalAsync(TradeCost cost, Tradeboard tradeboard,
			CancellationToken cancellationToken)
		{
			TradePayError? error;
			using (tradeboard.FetchModeScope())
			{
				if (!CanFetchInternal(cost, tradeboard, out error))
					return error;
			}

			foreach (var actualCost in cost.EnumerateActual(tradeboard))
			{
				if (actualCost is IInterceptableTradeCost interceptable)
				{
					var result = await interceptable.InterceptAsync(tradeboard, cancellationToken);

					if (result == InterceptResult.Cancel)
						return TradePayError.Cancelled;
				}
			}

			using (tradeboard.FetchModeScope())
			{
				using (ListPool<ITradeReceipt>.Get(out var receipts))
				{
					foreach (var actualCost in cost.EnumerateActual(tradeboard))
					{
						try
						{
							if (actualCost is not ITradeCostWithReceipt tradeCostWithReceipt)
								continue;

							if (cancellationToken.IsCancellationRequested)
								return TradePayError.Cancelled;

							var receipt = await tradeCostWithReceipt.FetchAsync(tradeboard, cancellationToken);

							if (cancellationToken.IsCancellationRequested)
								return TradePayError.Cancelled;

							if (receipt != null)
							{
								if (receipt.NeedPush())
									receipts.Add(receipt);
							}
							else
								return TradePayError.NotImplemented; //TODO: ошибка получения чека
						}
						catch (OperationCanceledException)
						{
							return TradePayError.Cancelled;
						}
						catch (Exception e)
						{
							throw TradingDebug.Exception(e.Message);
						}
					}

					if (receipts.Count > 0)
					{
						tradeboard.Register(receipts.ToArray());

						//_service.PushReceipts(tradeboard);
					}
				}

				return TradeAccess.CanPay(cost, tradeboard, out error) ? null : error;
			}
		}
	}
}
