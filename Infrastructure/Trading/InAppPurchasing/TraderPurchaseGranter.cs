using Content;
using Cysharp.Threading.Tasks;
using InAppPurchasing;
using Sapientia.Collections;
using UnityEngine.Scripting;

namespace Trading.InAppPurchasing
{
	[Preserve]
	public class TraderPurchaseGranter : IAPPurchaseGranter
	{
		private ITradingService _service;

		private HashMap<IAPProductEntry, TraderOfferReference> _iapProductToOffer;

		protected override void OnInitialize()
		{
			TradeManager.GetService(out _service);

			LinkIapProductsToTradePair();
		}

		public override bool Grant(in PurchaseReceipt receipt)
		{
			var entry = receipt.ToEntry();

			if (_iapProductToOffer.Contains(entry))
			{
				ref readonly var offer = ref _iapProductToOffer[entry];
				var pooledTradeboard = _service.CreateTradeboard(in offer.trader, out var tradeboard);
				var registerToken = tradeboard.Register(in receipt);

				if (!offer.CanExecute(tradeboard, out var error))
				{
					TradingDebug.LogError(
						$"Error on can execute trader offer [ {offer} ]: {error}");
					registerToken.Release();
					return false;
				}

				ExecuteAsync(offer, tradeboard)
				   .ContinueWith(Release)
				   .Forget();

				return true;

				void Release()
				{
					registerToken.Release();
					pooledTradeboard.Dispose();
				}
			}

			return false;
		}

		private async UniTask ExecuteAsync(TraderOfferReference offer, Tradeboard tradeboard)
		{
			var error = await offer.ExecuteAsync(tradeboard);
			if (error != null)
			{
				TradingDebug.LogError($"Error on execute trader offer [ {offer} ]: {error}");
				return;
			}

			var success = _service.PushCompleteOffer(in offer);

			if (!success)
				TradingDebug.LogError($"Error on push trader offer [ {offer} ]");
		}

		private void LinkIapProductsToTradePair()
		{
			_iapProductToOffer = new();

			foreach (var traderContentEntry in ContentManager.GetAllEntries<TraderConfig>())
			{
				var traderReference = traderContentEntry.ToReference();

				ref readonly var traderEntry = ref traderContentEntry.Value;

				foreach (var catalogReference in traderEntry.catalogs)
				{
					ref readonly var catalogEntry = ref catalogReference.Read();

					var i = 0;
					foreach (ref var offerEntry in catalogEntry)
					{
						var tradeEntry = offerEntry.trade.Read();
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

							if (!_iapProductToOffer.Contains(productEntry))
								_iapProductToOffer.SetOrAdd(productEntry, new TraderOfferReference(traderReference, catalogReference, i));
							else
								TradingDebug.LogError($"IAP product already registered [ {productEntry} ]");
						}

						i++;
					}
				}
			}
		}
	}
}
