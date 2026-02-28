#if CLIENT
using System;
using System.Collections.Generic;
using Sapientia.Extensions;
using Sapientia.Extensions.Reflection;
using Sirenix.OdinInspector;

namespace Trading
{
	[TypeRegistryItem(
		"\u2009Collection",
		"/",
		SdfIconType.Stack,
		darkIconColorR: R, darkIconColorG: G, darkIconColorB: B,
		darkIconColorA: A,
		lightIconColorR: R, lightIconColorG: G, lightIconColorB: B,
		lightIconColorA: A)]
	public partial class TradeRewardCollection : ITradeRewardRepresentable
	{
		/// <summary>
		/// Фильтрует типы только в инспекторе!
		/// </summary>
		public bool Filter(Type type) => type.HasAttribute<SerializableAttribute>();

		public string visual;
		public string VisualId { get => visual; }

		protected internal override IEnumerable<TradeRewardDrop> OnEnumerateDrop(Tradeboard board, TradeRewardDrop parent)
		{
			if (visual.IsNullOrEmpty() || ITradeRewardRepresentableWithCount.IsVisualIgnore(board))
			{
				foreach (var item in items)
				{
					if (item == null)
					{
						TradingDebug.LogError($"Null reward in collection (tradeId: {board.Id})");
						continue;
					}

					foreach (var reward in item.OnEnumerateDrop(board, new TradeRewardDrop(this, parent)))
						yield return reward;
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
