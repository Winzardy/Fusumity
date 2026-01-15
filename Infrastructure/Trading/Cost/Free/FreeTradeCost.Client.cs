#if CLIENT
using Sirenix.OdinInspector;

namespace Trading
{
	[TypeRegistryItem(
		"\u2009Free",
		"/",
		SdfIconType.Gift,
		darkIconColorR: R, darkIconColorG: G, darkIconColorB: B, darkIconColorA: A,
		lightIconColorR: R, lightIconColorG: G, lightIconColorB: B, lightIconColorA: A,
		priority: 6)]
	public sealed partial class FreeTradeCost
	{
	}
}
#endif
