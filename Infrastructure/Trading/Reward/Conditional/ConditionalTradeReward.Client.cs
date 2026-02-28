#if CLIENT
using System.Collections.Generic;
using Sapientia.Conditions;
using Sirenix.OdinInspector;

namespace Trading
{
	[TypeRegistryItem(
		"\u2009Condition",
		"/",
		SdfIconType.BagCheck,
		darkIconColorR: R, darkIconColorG: G, darkIconColorB: B,
		darkIconColorA: A,
		lightIconColorR: R, lightIconColorG: G, lightIconColorB: B,
		lightIconColorA: A)]
	public partial class ConditionalTradeReward
	{
		protected internal override IEnumerable<TradeRewardDrop> OnEnumerateDrop(Tradeboard board, TradeRewardDrop parent)
		{
			if (!condition.IsFulfilled(board))
				yield break;

			foreach (var actualReward in reward.OnEnumerateDrop(board, parent))
				yield return actualReward;
		}
	}
}
#endif
