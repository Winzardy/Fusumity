#if CLIENT
using System.Collections.Generic;
using Sapientia.Deterministic;
using Sirenix.OdinInspector;

namespace Trading
{
	[TypeRegistryItem(
		"\u2009Reward By Chance",
		"Random",
		SdfIconType.Dice1Fill,
		darkIconColorR: R, darkIconColorG: G, darkIconColorB: B,
		darkIconColorA: A,
		lightIconColorR: R, lightIconColorG: G, lightIconColorB: B,
		lightIconColorA: A)]
	public partial class TradeRewardByChance
	{
		protected internal override IEnumerable<TradeRewardDrop> OnEnumerateDrop(Tradeboard board, TradeRewardDrop parent)
		{
			var parentRate = parent.IsEmpty() ? Fix64.One : parent.rate;
			var rate = chance.Evaluate(board);
			yield return new TradeRewardDrop(this, rate * parentRate);
		}
	}
}
#endif
