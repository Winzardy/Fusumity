#if CLIENT
using System;
using Sirenix.OdinInspector;

namespace Trading
{
	[TypeRegistryItem(
		"\u2009Options - \ud83d\udea7 " + WARNING_MESSAGE,
		"",
		SdfIconType.UiChecksGrid,
		darkIconColorR: R, darkIconColorG: G, darkIconColorB: B, darkIconColorA: A,
		lightIconColorR: R, lightIconColorG: G, lightIconColorB: B, lightIconColorA: A,
		priority: 6)]
	[Obsolete(WARNING_MESSAGE)]
	public sealed partial class TradeCostOptions
	{
		internal const string WARNING_MESSAGE = "не поддерживается (пока)";
	}
}
#endif
