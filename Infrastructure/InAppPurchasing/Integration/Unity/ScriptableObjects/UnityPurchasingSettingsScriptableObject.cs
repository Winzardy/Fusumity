using Fusumity.Collections;
using InAppPurchasing;
using InAppPurchasing.Unity;
using ProjectInformation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.ScriptableObjects.InAppPurchasing
{
	[CreateAssetMenu(menuName = ContentIAPEditorConstants.CREATE_MENU + "Settings/UnityPurchasing",
		fileName = "IAP_Settings_UnityPurchasing")]
	public class UnityPurchasingSettingsScriptableObject : SingleContentEntryScriptableObject<UnityPurchasingSettings>
	{
		[Space, DictionaryDrawerSettings(KeyLabel = "Store", ValueLabel = "Country To Platform")]
		public SerializableDictionary<DistributionEntry, SerializableDictionary<CountryEntry, IAPBillingEntry>> storeToCountryToPlatform;

		protected override void OnImport(ref UnityPurchasingSettings settings)
		{
			settings.storeToCountryToBilling = new();
			foreach (var (store, dictionary) in storeToCountryToPlatform)
				settings.storeToCountryToBilling[store] = dictionary;
		}
	}
}
