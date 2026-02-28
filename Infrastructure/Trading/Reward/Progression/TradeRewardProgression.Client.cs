#if CLIENT
using System.Collections.Generic;
using Sapientia.Extensions;
using Sirenix.OdinInspector;

namespace Trading
{
	[TypeRegistryItem(
		"\u2009Progression",
		"/",
		SdfIconType.SortUp,
		darkIconColorR: R, darkIconColorG: G, darkIconColorB: B, darkIconColorA: A,
		lightIconColorR: R, lightIconColorG: G, lightIconColorB: B,
		lightIconColorA: A,
		priority: 5)]
	public sealed partial class TradeRewardProgression : ITradeRewardRepresentable
	{
		/// <remarks>
		/// Необходимо для Odin <c>[SerializeReference]</c>, без публичного пустого конструктора не даст создавать экземпляры в инспекторе
		/// </remarks>
		public TradeRewardProgression()
		{
		}

		public string visual;
		public string VisualId { get => visual; }

		protected internal override IEnumerable<TradeRewardDrop> OnEnumerateDrop(Tradeboard board, TradeRewardDrop parent)
		{
			if (visual.IsNullOrEmpty() && ITradeRewardRepresentableWithCount.IsVisualIgnore(board))
			{
				foreach (var reward in GetCurrentStage(board)
					.reward
					.OnEnumerateDrop(board, parent))
					yield return reward;
			}
			else
			{
				yield return new TradeRewardDrop(this, parent);
			}
		}
	}
}
#endif
