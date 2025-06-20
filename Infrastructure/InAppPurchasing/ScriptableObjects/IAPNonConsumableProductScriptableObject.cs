using Fusumity.Collections;
using InAppPurchasing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Content.ScriptableObjects.InAppPurchasing
{
	[CreateAssetMenu(menuName = ContentIAPEditorConstants.CREATE_MENU + "Product/Non Consumable",
		fileName = "IAP_Product_NonConsumable_New")]
	public class IAPNonConsumableProductScriptableObject : ContentEntryScriptableObject<IAPNonConsumableProductEntry>
	{
		[FormerlySerializedAs("platformToId")]
		[Space, DictionaryDrawerSettings(KeyLabel = "Billing", ValueLabel = "Id")]
		[Tooltip("[High Priority] Billing <-> Id, если магазина нет в словаре, то будет использовать 'Custom' или стандартны Id")]
		public SerializableDictionary<IAPBillingEntry, string> billingToId;

		protected override void OnImport(ref IAPNonConsumableProductEntry product)
		{
			product.billingToId = billingToId;
		}
	}
}
