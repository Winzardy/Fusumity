using Fusumity.Collections;
using InAppPurchasing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Content.ScriptableObjects.InAppPurchasing
{
	[CreateAssetMenu(menuName = ContentIAPEditorConstants.CREATE_MENU + "Product/Consumable", fileName = "IAP_Product_Consumable_New")]
	public class IAPConsumableProductScriptableObject : ContentEntryScriptableObject<IAPConsumableProductEntry>
	{
		[FormerlySerializedAs("platformToId")]
		[Space, DictionaryDrawerSettings(KeyLabel = "Billing", ValueLabel = "Id")]
		[Tooltip("[High Priority] Billing <-> Id, если магазина нет в словаре, то будет использовать 'Custom' или стандартны Id")]
		public SerializableDictionary<IAPBillingEntry, string> billingToId;

		protected override void OnImport(ref IAPConsumableProductEntry product)
		{
			product.billingToId = billingToId;
		}
	}
}
