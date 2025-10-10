using Trading;
using UnityEngine;

namespace Content.ScriptableObjects.Trading
{
	[CreateAssetMenu(menuName = ContentTradingEditorConstants.CREATE_MENU + "Catalog", fileName = "TradeCatalog_New")]
	public class TradeCatalogScriptableObject : ContentEntryScriptableObject<TradeCatalogConfig>
	{
	}
}
