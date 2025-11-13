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
	// TODO: Убрать TradeManager. Нужную часть кода перенести в TradeUtility
	/// <summary>
	/// Только для Client'a
	/// </summary>
	public class TradeManagement : ITradeManagement
	{
		// Заглушка только для offline режима
		private ITradingNode _dummyNode;

		private readonly ITradingService _service;

		public TradeManagement(ITradingService service)
		{
			_service = service;
		}

		public bool CanFetchOrExecute([CanBeNull] TradeCost cost, Tradeboard tradeboard, out TradePayError? error)
		{
			if (!tradeboard.IsFetchMode)
				throw new ArgumentException("Tradeboard will be fetched!");

			error = null;

			if (cost == null)
				return false;

			foreach (var t in cost.EnumerateActual(tradeboard))
			{
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

			return true;
		}

		public bool CanFetchOrExecute(in TradeConfig trade, Tradeboard tradeboard, out TradeExecuteError? error)
		{
			if (!tradeboard.IsFetchMode)
				throw new ArgumentException("Tradeboard will be fetched!");

			error = null;

			TradePayError? payError = null;
			foreach (var actualTradeCost in trade.cost.EnumerateActual(tradeboard))
			{
				if (actualTradeCost is ITradeCostWithReceipt tradeCostWithReceipt)
				{
					if (!tradeCostWithReceipt.CanFetch(tradeboard, out payError))
					{
						payError ??= TradePayError.NotImplemented;
						break;
					}
				}
				else
				{
					if (!actualTradeCost.CanExecute(tradeboard, out payError))
					{
						payError ??= TradePayError.NotImplemented;
						break;
					}
				}
			}

			if (payError.HasValue)
			{
				error = new TradeExecuteError(payError, null);
				return false;
			}

			if (!trade.reward.CanExecute(tradeboard, out var receiveError))
			{
				error = new TradeExecuteError(payError, receiveError);
				return false;
			}

			return true;
		}

		public void GetService(out ITradingService service) => service = _service;

		public async Task<TradeExecuteError?> FetchAsync(TradeConfig trade, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			using (tradeboard.FetchModeScope())
			{
				if (!CanFetchOrExecute(in trade, tradeboard, out var error))
					return error;
			}

			var payError = await FetchAsync(trade.cost, tradeboard, cancellationToken);
			if (payError != null)
				return new TradeExecuteError(payError, null);

			return null;
		}

		/// <param name="fullPay">Нужно ли в конце выполнить Pay если ITradingBackend не задан</param>
		public async Task<TradePayError?> FetchAsync(TradeCost cost, Tradeboard tradeboard,
			CancellationToken cancellationToken)
		{
			TradePayError? error;
			using (tradeboard.FetchModeScope())
			{
				if (!CanFetchOrExecute(cost, tradeboard, out error))
					return error;
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
						using var _ = tradeboard.Register(receipts.ToArray());

						_service.PushReceipts(tradeboard);

						var cStr = receipts.GetCompositeString(true, getter:
							receipt => receipt.ToString(), numerate: receipts.Count > 1);
						TradingDebug.Log($"{tradeboard.Id}, receipts:{cStr}");
					}
				}

				return TradeAccess.CanPay(cost, tradeboard, out error) ? null : error;
			}
		}
	}
}
