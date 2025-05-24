using Fusumity.Collections;
using InAppPurchasing;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.ScriptableObjects.InAppPurchasing
{
	[CreateAssetMenu(menuName = ContentIAPEditorConstants.CREATE_MENU + "Product/Consumable", fileName = "IAP_Product_Consumable_New")]
	public class IAPConsumableProductScriptableObject : ContentEntryScriptableObject<IAPConsumableProductEntry>
	{
		[Space, DictionaryDrawerSettings(KeyLabel = "IAP Platform", ValueLabel = "Id")]
		[Tooltip("[High Priority] Platform <-> Id, если магазина нет в словаре, то будет использовать 'Custom' или стандартны Id")]
		public SerializableDictionary<IAPPlatformEntry, string> platformToId;

		protected override void OnImport(ref IAPConsumableProductEntry product)
		{
			product.platformToId = platformToId;
		}
	}
}
