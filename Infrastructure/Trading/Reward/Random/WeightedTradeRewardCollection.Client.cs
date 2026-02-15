#if CLIENT
using System;
using System.Collections.Generic;
using Sapientia;
using Sapientia.Evaluators;
using Sapientia.Extensions.Reflection;
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
	}
}
#endif
