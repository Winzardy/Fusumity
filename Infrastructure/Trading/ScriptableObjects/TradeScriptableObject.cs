using Trading;
using UnityEngine;

namespace Content.ScriptableObjects.Trading
{
	[CreateAssetMenu(menuName = ContentTradingEditorConstants.CREATE_MENU + "Trade", fileName = "Trade_New")]
	public class TradeScriptableObject : ContentEntryScriptableObject<TradeConfig>
	{
	}
}
