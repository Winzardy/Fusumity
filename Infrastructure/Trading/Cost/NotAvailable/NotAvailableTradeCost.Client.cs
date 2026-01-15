#if CLIENT
using Sirenix.OdinInspector;

namespace Trading
{
	[TypeRegistryItem(
		"\u2009Not available",
		"/",
		SdfIconType.XOctagon,
		darkIconColorR: R, darkIconColorG: G, darkIconColorB: B, darkIconColorA: A,
		lightIconColorR: R, lightIconColorG: G, lightIconColorB: B, lightIconColorA: A,
		priority: 6)]
	public sealed partial class NotAvailableTradeCost
	{
	}
}
#endif
