using AssetManagement;
using Content;
using JetBrains.Annotations;
using Localizations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Trading
{
	public partial class TradeCatalogEntry
	{
		[PropertySpace(0, 10)]
		[ClientOnly]
		public LocKey name;
	}

	public partial struct TraderOfferEntry
	{
		[ClientOnly]
		public LocKey name;

		[ClientOnly]
		public AssetReferenceEntry<Sprite> icon;

		[ClientOnly, CanBeNull]
		public LocKey label;

		[ClientOnly, HideInInspector]
		public bool disableConfirmation;

		public bool UseConfirmation { get => !disableConfirmation; set => disableConfirmation = !value; }

		public Sprite Preview => icon.editorAsset;
	}
}
