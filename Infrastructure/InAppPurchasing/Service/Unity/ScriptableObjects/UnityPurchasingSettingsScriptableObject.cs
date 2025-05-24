using Fusumity.Collections;
using InAppPurchasing;
using InAppPurchasing.Unity;
using Sirenix.OdinInspector;
using Targeting;
using UnityEngine;

namespace Content.ScriptableObjects.InAppPurchasing
{
	[CreateAssetMenu(menuName = ContentIAPEditorConstants.CREATE_MENU + "Settings/UnityPurchasing",
		fileName = "IAP_Settings_UnityPurchasing")]
	public class UnityPurchasingSettingsScriptableObject : SingleContentEntryScriptableObject<UnityPurchasingSettings>
	{
		[Space, DictionaryDrawerSettings(KeyLabel = "Store", ValueLabel = "Country To Platform")]
		public SerializableDictionary<StorePlatformEntry, SerializableDictionary<CountryEntry, IAPPlatformEntry>> storeToCountryToPlatform;

		protected override void OnImport(ref UnityPurchasingSettings settings)
		{
			settings.storeToCountryToPlatform = new();
			foreach (var (store, dictionary) in storeToCountryToPlatform)
				settings.storeToCountryToPlatform[store] = dictionary;
		}
	}
}
