using AssetManagement;
using Content;
using Localizations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Trading
{
	public partial class TradeCatalogEntry
	{
		[PropertySpace(0,10)]
		[ClientOnly]
		public LocKey name;
	}

	public partial struct TraderOfferEntry
	{
		[ClientOnly]
		public LocKey name;
		[ClientOnly]
		public AssetReferenceEntry<Sprite> icon;

		public Sprite Preview => icon.editorAsset;
	}
}
