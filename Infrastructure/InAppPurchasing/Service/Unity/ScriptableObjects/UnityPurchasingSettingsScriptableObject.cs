using Distribution;
using InAppPurchasing;
using InAppPurchasing.Unity;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.ScriptableObjects.InAppPurchasing
{
	[CreateAssetMenu(menuName = ContentIAPEditorConstants.CREATE_MENU + "Settings/UnityPurchasing",
		fileName = "IAP_Settings_UnityPurchasing")]
	public class UnityPurchasingSettingsScriptableObject : SingleContentEntryScriptableObject<UnityPurchasingSettings>
	{
		[Space, DictionaryDrawerSettings(KeyLabel = "Store", ValueLabel = "Country To Platform")]
		public SerializableDictionary<StorePlatformEntry, SerializableDictionary<CountryEntry, IAPPlatformEntry>> storeToCountryToPlatform;

		protected override void OnImport()
		{
			Edit(OnEdit);

			void OnEdit(ref UnityPurchasingSettings settings)
			{
				settings.storeToCountryToPlatform = new();
				foreach (var (store, dictionary) in storeToCountryToPlatform)
					settings.storeToCountryToPlatform[store] = dictionary;
			}
		}
	}
}
