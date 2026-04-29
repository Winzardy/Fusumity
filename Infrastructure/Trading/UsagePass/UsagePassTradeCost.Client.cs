#if CLIENT
using Sapientia;
using Sirenix.OdinInspector;

namespace Trading.UsagePass
{
	[TypeRegistryItem(
		"\u2009Usage Pass", //В начале делаем отступ из-за отрисовки...
		"/",
		SdfIconType.CalendarCheck,
		darkIconColorR: R, darkIconColorG: G, darkIconColorB: B, darkIconColorA: A,
		lightIconColorR: R, lightIconColorG: G, lightIconColorB: B, lightIconColorA: A)]
	public partial class UsagePassTradeCost
	{
		public bool TryGetUsagePassState(Tradeboard tradeboard, out int current, out int total)
		{
			if (limit.usageCount <= 0)
			{
				current = 0;
				total   = 0;
				return false;
			}

			total = limit.usageCount; // <- total

			if (total == 0)
			{
				current = 0;
				return false;
			}

			var key = UsagePassTradeReceiptUtility.ToRecipeKey(tradeboard.Id);
			var usagePassNode = tradeboard.Get<IUsagePassNode>();
			ref readonly var state = ref usagePassNode.GetState(key);

			current = total - limit.GetRemainingUsages(in state); // <- current
			return true;
		}
	}
}
#endif
