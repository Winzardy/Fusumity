#if CLIENT
using Sirenix.OdinInspector;

namespace Trading
{
	[TypeRegistryItem(
		"\u2009Change Trade Progress",
		"/",
		SdfIconType.BagPlusFill,
		darkIconColorR: R, darkIconColorG: G, darkIconColorB: B, darkIconColorA: A,
		lightIconColorR: R, lightIconColorG: G, lightIconColorB: B,
		lightIconColorA: A)]
	public sealed partial class TradeChangeProgressReward
	{
		/// <remarks>
		/// Необходимо для Odin <c>[SerializeReference]</c>, без публичного пустого конструктора не даст создавать экземпляры в инспекторе
		/// </remarks>
		public TradeChangeProgressReward()
		{
		}
	}
}
#endif
