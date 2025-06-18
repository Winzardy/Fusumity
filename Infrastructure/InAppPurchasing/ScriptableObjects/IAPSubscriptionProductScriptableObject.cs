using Fusumity.Collections;
using InAppPurchasing;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.ScriptableObjects.InAppPurchasing
{
	[CreateAssetMenu(menuName = ContentIAPEditorConstants.CREATE_MENU + "Product/Subscription", fileName = "IAP_Product_Subscription_New")]
	public class IAPSubscriptionProductScriptableObject : ContentEntryScriptableObject<IAPSubscriptionProductEntry>
	{
		[Space, DictionaryDrawerSettings(KeyLabel = "Billing", ValueLabel = "Id")]
		[Tooltip("[High Priority] Billing <-> Id, если магазина нет в словаре, то будет использовать 'Custom' или стандартны Id")]
		public SerializableDictionary<IAPBillingEntry, string> billingToId;

		protected override void OnImport(ref IAPSubscriptionProductEntry product)
		{
			product.billingToId = billingToId;
		}
	}
}
