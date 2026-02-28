#if CLIENT
using System;
using System.Collections.Generic;
using Sapientia;
using Sapientia.Collections;
using Sapientia.Deterministic;
using Sapientia.Evaluators;
using Sapientia.Extensions;
using Sapientia.Extensions.Reflection;
using Sapientia.Pooling;
using Sirenix.OdinInspector;

namespace Trading
{
	[TypeRegistryItem(
		"\u2009Reward By Weight",
		"Random",
		SdfIconType.Dice5Fill,
		darkIconColorR: R, darkIconColorG: G, darkIconColorB: B,
		darkIconColorA: A,
		lightIconColorR: R, lightIconColorG: G, lightIconColorB: B,
		lightIconColorA: A)]
	public partial class WeightedTradeRewardCollection : ITradeRewardRepresentableWithCount
	{
		/// <summary>
		/// Фильтрует типы только в инспекторе!
		/// </summary>
		public bool Filter(Type type) =>
			!typeof(IEnumerable<TradeReward>).IsAssignableFrom(type) && type.HasAttribute<SerializableAttribute>();

		public bool CanShowRollMode()
		{
			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (count.evaluator != null)
				return true;

			return count.value > 1;
		}

		public string visual;
		public string VisualId { get => visual; }
		ref readonly EvaluatedValue<Blackboard, int> ITradeRewardRepresentableWithCount.Count { get => ref count; }

		protected internal override IEnumerable<TradeRewardDrop> OnEnumerateDrop(Tradeboard board, TradeRewardDrop parent)
		{
			if (visual.IsNullOrEmpty() || ITradeRewardRepresentableWithCount.IsVisualIgnore(board))
			{
				using (ListPool<int>.Get(out var weightByItem))
				{
					items.Fill(board as Blackboard, weightByItem);
					var totalWeight = weightByItem.TotalWeight();
					foreach (var (item, i) in items.WithIndex())
					{
						var rate = (Fix64) weightByItem[i] / totalWeight;
						rate *= parent.rate;
						foreach (var drop in item.reward.OnEnumerateDrop(board, new TradeRewardDrop(item.reward, rate)))
							yield return drop;
					}
				}
			}
			else
			{
				yield return new TradeRewardDrop(this, parent);
			}
		}
	}
}
#endif
