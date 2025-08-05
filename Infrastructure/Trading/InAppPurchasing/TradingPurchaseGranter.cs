using Content;
using Cysharp.Threading.Tasks;
using InAppPurchasing;
using Sapientia.Collections;
using Sapientia.Pooling;
using UnityEngine.Scripting;

namespace Trading.InAppPurchasing
{
	using TradeReference = ContentReference<TradeEntry>;
	using TraderReference = ContentReference<TraderEntry>;

	[Preserve]
	public class TradingPurchaseGranter : IAPPurchaseGranter
	{
		private HashMap<IAPProductEntry, Pair> _iapProductToTradePair;

		protected override void OnInitialize()
		{
			LinkIapProductsToTradePair();
		}

		public override bool Grant(in PurchaseReceipt receipt)
		{
			var entry = receipt.ToEntry();

			if (_iapProductToTradePair.Contains(entry))
			{
				var pair = _iapProductToTradePair[entry];

				var trader = pair.trader;
				var trade = pair.trade;

				var pooledTradeboard = TradeManager.CreateTradeboard(in trader);
				if (pooledTradeboard == null)
					return false;

				if (!trade.CanExecute(pooledTradeboard, out var error))
				{
					TradingDebug.LogError($"Error on can execute trade {trade.ToLabel()} in trader {trader.ToLabel()}: {error}");
					return false;
				}

				ExecuteAsync(trader, trade, receipt, pooledTradeboard.Value).Forget();
				return true;
			}

			return false;
		}

		private async UniTaskVoid ExecuteAsync(TraderReference trader, TradeReference trade, PurchaseReceipt receipt,
			PooledObject<Tradeboard> pooledTradeboard)
		{
			using (pooledTradeboard)
			{
				Tradeboard tradeboard = pooledTradeboard;
				tradeboard.Register(in receipt);

				var error = await trade.ExecuteAsync(tradeboard);
				if (error != null)
				{
					TradingDebug.LogError($"Error on execute trade {trade.ToLabel()} in trader {trader.ToLabel()}: {error}");
					return;
				}

				// Значит оффлайн режим и service не задан, знаю что не супер очевидно, пока так, лучше не придумал
				if (tradeboard.Contains<DummyTradingBackend>())
					return;

				var success = TradeManager.PushTrade(in trader, in trade);

				if (!success)
					TradingDebug.LogError($"Error on push trade {trade.ToLabel()} in trader {trader.ToLabel()}");
			}
		}

		private void LinkIapProductsToTradePair()
		{
			_iapProductToTradePair = new();
			using (HashSetPool<IAPProductEntry>.Get(out var hashSet))
			{
				foreach (var traderContentEntry in ContentManager.GetAll<TraderEntry>())
				{
					var traderReference = traderContentEntry.ToReference();

					ref readonly var traderEntry = ref traderContentEntry.Value;

					foreach (var catalogReference in traderEntry.catalogs)
					{
						ref readonly var catalogEntry = ref catalogReference.Read();

						var i = 0;
						foreach (ref var tradeReference in catalogEntry)
						{
							var tradeEntry = tradeReference.trade.Read();
							foreach (var cost in tradeEntry.cost)
							{
								if (cost is not IAPTradeCost iapTradeCost)
									continue;

								var productEntry = iapTradeCost.GetProductEntry();
								if (productEntry == null)
								{
									TradingDebug.LogWarning("Empty product entry in cost");
									continue;
								}

								if (hashSet.Add(productEntry))
									_iapProductToTradePair[productEntry] =
										new Pair(traderReference, tradeReference.trade);
								else
									TradingDebug.LogError($"IAP product already registered [ {productEntry} ]");
							}

							i++;
						}
					}
				}
			}
		}

		private struct Pair
		{
			public ContentReference<TraderEntry> trader;
			public ContentReference<TradeEntry> trade;

			public Pair(ContentReference<TraderEntry> trader, ContentReference<TradeEntry> trade)
			{
				this.trade = trade;
				this.trader = trader;
			}
		}
	}
}
