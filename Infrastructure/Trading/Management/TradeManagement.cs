using System;
using System.Threading;
using System.Threading.Tasks;
using Content;
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
		private ITradingBackend _dummyBackend;

		private readonly ITradingService _service;

		public TradeManagement(ITradingService service)
		{
			_service = service;
		}

		public bool CanPay(TradeCost cost, Tradeboard tradeboard, out TradePayError? error)
		{
			error = null;

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

		public Task<TradePayError?> PayAsync(TradeCost cost, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			return PayAsync(cost, tradeboard, cancellationToken, true);
		}

		public bool CanExecute(in TradeEntry trade, Tradeboard tradeboard, out TradeExecuteError? error)
		{
			error = null;

			TradePayError? payError = null;
			foreach (var t in trade.cost)
			{
				if (t is ITradeCostWithReceipt tradeCostWithReceipt)
				{
					if (!tradeCostWithReceipt.CanFetch(tradeboard, out payError))
						break;
				}
				else
				{
					if (!t.CanExecute(tradeboard, out payError))
						break;
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

		public async Task<TradeExecuteError?> ExecuteAsync(TradeEntry trade, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			if (!CanExecute(in trade, tradeboard, out var error))
				return error;

			var payError = await PayAsync(trade.cost, tradeboard, cancellationToken, false);

			if (payError != null)
				return new TradeExecuteError(payError, null);

			// Если Service не задан значит offline режим, можно было перенести это в кастомный сервис
			if (_service != null)
				return TradeExecuteError.NotError;

			_dummyBackend ??= new DummyTradingBackend();
			using var _ = tradeboard.Register(_dummyBackend);

			// Если Verification не задан значит нет предварительных этапов и так далее, грубо говоря оффлайн режим
			if (TradeAccess.Execute(in trade, tradeboard))
				return TradeExecuteError.NotError;

			return TradeExecuteError.NotImplemented;
		}

		public void GetService(out ITradingService service) => service = _service;

		/// <param name="fullPay">Нужно ли в конце выполнить Pay если ITradingBackend не задан</param>
		private async Task<TradePayError?> PayAsync(TradeCost cost, Tradeboard tradeboard,
			CancellationToken cancellationToken, bool fullPay)
		{
			if (!CanPay(cost, tradeboard, out var error))
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

					_service?.PushReceipts(tradeboard);

					var cStr = receipts.GetCompositeString(true, getter:
						receipt => receipt.ToString(), numerate: receipts.Count > 1);
					TradingDebug.Log($"{tradeboard.Id}, receipts:{cStr}");
				}
			}

			if (_service != null || !fullPay)
				return TradeAccess.CanPay(cost, tradeboard, out error) ? null : error;

			_dummyBackend ??= new DummyTradingBackend();
			using var __ = tradeboard.Register(_dummyBackend);

			// Если Verification не задан значит нет предварительных этапов и так далее, грубо говоря оффлайн режим
			if (TradeAccess.Pay(cost, tradeboard))
				return TradePayError.NotError;

			return TradePayError.NotImplemented;
		}
	}

	// TODO: не нравится, убрать...
	public class DummyTradingBackend : ITradingBackend
	{
		private UsageLimitModel _empty;
		public ITradeReceiptRegistry<T> GetRegistry<T>() where T : struct, ITradeReceipt => null;
		public ref UsageLimitModel GetUsageModel(SerializableGuid guid) => ref _empty;
	}

	public interface ITradingServiceFactory
	{
		ITradingService Create();
	}
}
