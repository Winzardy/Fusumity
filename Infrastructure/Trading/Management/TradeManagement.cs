using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Trading;

namespace Game.App.BootTask
{
	public interface ITradeVerification
	{
		public void Submit(Tradeboard tradeboard);
	}

	public class TradeManagement : ITradeManagement
	{
		private readonly ITradeVerification _verification;

		public TradeManagement(ITradeVerification tradeVerification)
		{
			_verification = tradeVerification;
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

		public async Task<TradePayError?> PayAsync(TradeCost cost, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
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
						return new TradePayError(); //TODO: остановка

					var receipt = await tradeCostWithReceipt.FetchAsync(tradeboard, cancellationToken);

					if (cancellationToken.IsCancellationRequested)
						return new TradePayError(); //TODO: остановка

					if (receipt != null)
						receipts.Add(receipt);
					else
						return new TradePayError(); //TODO: ошибка получения чека
				}

				if (receipts.Count > 0)
				{
					// Отправить чеки, так же возможно придется шифровать чеки,
					// возможно нет смысла если будет общий механизм шифрования команд
					tradeboard.Register(receipts.ToArray());

					_verification?.Submit(tradeboard);

					var compositeString = receipts.GetCompositeString(false, getter:
						receipt => receipt.ToString());
					TradingDebug.Log($"{tradeboard.Id}, receipts: {compositeString}");
				}
			}

			if (_verification != null)
				return null;

			tradeboard.Register<ITradingModel>(new DummyTradingRepository());

			// Если Verification не задан значит нет предварительных этапов и так далее, грубо говоря оффлайн режим
			if (TradeAccess.Pay(cost, tradeboard))
				return null;

			return new TradePayError();
		}
	}

	public class DummyTradingRepository : ITradingModel
	{
		public ITradeReceiptRegistry<T> Get<T>() where T : struct, ITradeReceipt => null;
	}
}
