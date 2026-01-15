#if CLIENT
using Sirenix.OdinInspector;

namespace Trading
{
	[TypeRegistryItem(
		"\u2009Collection",
		"/",
		SdfIconType.Stack,
		darkIconColorR: R, darkIconColorG: G, darkIconColorB: B, darkIconColorA: A,
		lightIconColorR: R, lightIconColorG: G, lightIconColorB: B,
		lightIconColorA: A,
		priority: 5)]
	public sealed partial class TradeCostCollection
	{
		/// <remarks>
		/// Необходимо для Odin <c>[SerializeReference]</c>, без публичного пустого конструктора не даст создавать экземпляры в инспекторе
		/// </remarks>
		public TradeCostCollection()
		{
		}
	}
}
#endif
