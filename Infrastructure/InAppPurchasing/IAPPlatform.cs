using Content;

namespace InAppPurchasing
{
	public abstract partial class IAPProductEntry
	{
		[ClientOnly]
		public string titleLocKey;
		[ClientOnly]
		public string descriptionLocKey;
	}
}
