using System.Threading;
using System.Threading.Tasks;
using Content;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Trading;

namespace Game.App.BootTask
{
	using TraderReference = ContentReference<TraderEntry>;
	using TradeReference = ContentReference<TradeEntry>;

	public interface ITradingBackend
	{
		/// <summary>
		/// Отправить детали сделки в backend
		/// </summary>
		public void PushReceipts(Tradeboard tradeboard);

		public bool PushTrade(in TraderReference trader, in TradeReference trade);
	}

	public class TradeManagement : ITradeManagement
	{
		private readonly ITradingBackend _backend;

		public TradeManagement(ITradingBackend backend)
		{
			_backend = backend;
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

			if (!trade.reward.CanReceive(tradeboard, out var receiveError))
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

			if (_backend != null)
				return null;

			tradeboard.Register<ITradingModel>(new DummyTradingModel());

			// Если Verification не задан значит нет предварительных этапов и так далее, грубо говоря оффлайн режим
			if (TradeAccess.Execute(in trade, tradeboard))
				return null;

			return TradeExecuteError.NotImplemented;
		}

		/// <param name="full">Нужно ли в конце выполнить Pay если IExternalTradingModel не задан</param>
		private async Task<TradePayError?> PayAsync(TradeCost cost, Tradeboard tradeboard,
			CancellationToken cancellationToken, bool full)
		{
			if (!CanPay(cost, tradeboard, out var error))
				return error;

			using (ListPool<ITradeReceipt>.Get(out var receipts))
			{
				foreach (var t in cost)
				{
					if (t is not ITradeCostWithReceipt tradeCostWithReceipt)
						continue;

					if (cancellationToken.IsCancellationRequested)
						return TradePayError.NotImplemented; //TODO: остановка

					var receipt = await tradeCostWithReceipt.FetchAsync(tradeboard, cancellationToken);

					if (cancellationToken.IsCancellationRequested)
						return TradePayError.NotImplemented; //TODO: остановка

					if (receipt != null)
						receipts.Add(receipt);
					else
						return TradePayError.NotImplemented; //TODO: ошибка получения чека
				}

				if (receipts.Count > 0)
				{
					tradeboard.Register(receipts.ToArray());

					_backend?.PushReceipts(tradeboard);

					var compositeString = receipts.GetCompositeString(false, getter:
						receipt => receipt.ToString());
					TradingDebug.Log($"{tradeboard.Id}, receipts: {compositeString}");
				}
			}

			if (_backend != null || !full)
				return null;

			tradeboard.Register<ITradingModel>(new DummyTradingModel());

			// Если Verification не задан значит нет предварительных этапов и так далее, грубо говоря оффлайн режим
			if (TradeAccess.Pay(cost, tradeboard))
				return null;

			return TradePayError.NotImplemented;
		}
	}

	public class DummyTradingModel : ITradingModel
	{
		public ITradeReceiptRegistry<T> Get<T>() where T : struct, ITradeReceipt => null;
	}
}
