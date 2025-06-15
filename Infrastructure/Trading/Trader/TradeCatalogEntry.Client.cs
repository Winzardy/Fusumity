using AssetManagement;
using Localizations;
using UnityEngine;

namespace Trading
{
	public partial class TradeCatalogEntry
	{
		public LocKey name;
	}

	public partial struct TradeOfferEntry
	{
		public LocKey name;
		public AssetReferenceEntry<Sprite> icon;
	}
}
