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
			error = null;

			if (cost == null)
				return false;
			foreach (var t in cost)
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
			error = null;

			TradePayError? payError = null;
			foreach (var t in trade.cost)
			{
				if (t is ITradeCostWithReceipt tradeCostWithReceipt)
				{
					if (!tradeCostWithReceipt.CanFetch(tradeboard, out payError))
					{
						payError ??= TradePayError.NotImplemented;
						break;
					}
				}
				else
				{
					if (!t.CanExecute(tradeboard, out payError))
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
			if (!CanFetchOrExecute(in trade, tradeboard, out var error))
				return error;

			var payError = await FetchAsync(trade.cost, tradeboard, cancellationToken);
			if (payError != null)
				return new TradeExecuteError(payError, null);
			return null;
		}

		/// <param name="fullPay">Нужно ли в конце выполнить Pay если ITradingBackend не задан</param>
		public async Task<TradePayError?> FetchAsync(TradeCost cost, Tradeboard tradeboard,
			CancellationToken cancellationToken)
		{
			if (!CanFetchOrExecute(cost, tradeboard, out var error))
				return error;

			using (ListPool<ITradeReceipt>.Get(out var receipts))
			{
				foreach (var t in cost)
				{
					try
					{
						if (t is not ITradeCostWithReceipt tradeCostWithReceipt)
							continue;

						if (cancellationToken.IsCancellationRequested)
							return TradePayError.NotImplemented; //TODO: остановка

						var receipt = await tradeCostWithReceipt.FetchAsync(tradeboard, cancellationToken);

						if (cancellationToken.IsCancellationRequested)
							return TradePayError.NotImplemented; //TODO: остановка

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

			// _dummyNode ??= new DummyTradingNode();
			// using var __ = tradeboard.Register(_dummyNode);
			//
			// // Если Verification не задан значит нет предварительных этапов и так далее, грубо говоря оффлайн режим
			// if (TradeAccess.Pay(cost, tradeboard))
			// 	return TradePayError.NotError;
			//
			// return TradePayError.NotImplemented;
		}
	}
}
