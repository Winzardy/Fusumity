using Content;
using Sapientia.Pooling;

namespace Trading
{
	public interface ITradingService
	{
		/// <summary>
		/// Отправить детали сделки в сервис
		/// </summary>
		/// <remarks>Если оффлайн, то отправлять не надо</remarks>
		public void PushReceipts(Tradeboard tradeboard)
		{
		}

		#region Trader

		/// <remarks>Если оффлайн, то отправлять не надо</remarks>
		public bool PushCompleteOffer(in TraderOfferReference offer) => true;

		public PooledObject<Tradeboard> CreateTradeboard(in ContentReference<TraderConfig> trader, out Tradeboard tradeboard);

		#endregion
	}

	public interface ITradingServiceFactory
	{
		ITradingService Create();
	}
}
