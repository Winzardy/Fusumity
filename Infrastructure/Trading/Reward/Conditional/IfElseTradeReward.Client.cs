#if CLIENT
using System.Collections.Generic;
using Sapientia.Conditions;
using Sirenix.OdinInspector;

namespace Trading
{
	[TypeRegistryItem(
		"\u2009If / else",
		"/",
		SdfIconType.Alt,
		darkIconColorR: R, darkIconColorG: G, darkIconColorB: B,
		darkIconColorA: A,
		lightIconColorR: R, lightIconColorG: G, lightIconColorB: B,
		lightIconColorA: A)]
	public partial class IfElseTradeReward
	{
		protected internal override IEnumerable<TradeRewardDrop> OnEnumerateDrop(Tradeboard board, TradeRewardDrop parent)
		{
			if (condition.IsFulfilled(board))
				foreach (var reward in a.OnEnumerateDrop(board, parent))
					yield return reward;
			else
				foreach (var reward in b.OnEnumerateDrop(board, parent))
					yield return reward;
		}
	}
}
#endif
