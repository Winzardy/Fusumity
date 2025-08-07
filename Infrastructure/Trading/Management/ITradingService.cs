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

		/// <remarks>Если оффлайн, то отправлять не надо</remarks>
		public bool PushOffer(in TraderOfferReference offer) => true;

		public PooledObject<Tradeboard> CreateTradeboard(in ContentReference<TraderEntry> trader);
	}
}
