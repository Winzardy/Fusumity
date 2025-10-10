using Trading;
using UnityEngine;

namespace Content.ScriptableObjects.Trading
{
	[CreateAssetMenu(menuName = ContentTradingEditorConstants.CREATE_MENU + "Trader", fileName = "Trader_New", order = 100)]
	public class TraderScriptableObject : ContentEntryScriptableObject<TraderConfig>
	{
	}
}
