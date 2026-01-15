#if CLIENT
using Sirenix.OdinInspector;

namespace Trading.InAppPurchasing
{
	[TypeRegistryItem(
		"\u2009Real Money", //В начале делаем отступ из-за отрисовки...
		"InAppPurchasing",
		SdfIconType.Cash,
		darkIconColorR: R, darkIconColorG: G, darkIconColorB: B, darkIconColorA: A,
		lightIconColorR: R, lightIconColorG: G, lightIconColorB: B, lightIconColorA: A)]
	public partial class IAPConsumableTradeCost
	{
	}
}
#endif
