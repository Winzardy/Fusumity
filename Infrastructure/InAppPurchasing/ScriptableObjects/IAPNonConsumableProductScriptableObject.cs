using InAppPurchasing;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.ScriptableObjects.InAppPurchasing
{
	[CreateAssetMenu(menuName = ContentIAPEditorConstants.CREATE_MENU + "Product/Non Consumable",
		fileName = "IAP_Product_NonConsumable_New")]
	public class IAPNonConsumableProductScriptableObject : ContentEntryScriptableObject<IAPNonConsumableProductEntry>
	{
		[Space, DictionaryDrawerSettings(KeyLabel = "IAP Platform", ValueLabel = "Id")]
		[Tooltip("[High Priority] Platform <-> Id, если магазина нет в словаре, то будет использовать 'Custom' или стандартны Id")]
		public SerializableDictionary<IAPPlatformEntry, string> platformToId;

		protected override void OnImport()
		{
			Value.platformToId = platformToId;
		}
	}
}
