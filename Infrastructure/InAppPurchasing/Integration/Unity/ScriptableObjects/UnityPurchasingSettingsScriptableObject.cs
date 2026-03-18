using System;
using Fusumity.Collections;
using InAppPurchasing;
using InAppPurchasing.Unity;
using ProjectInformation;
using Sapientia;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.ScriptableObjects.InAppPurchasing
{
	[CreateAssetMenu(menuName = ContentIAPEditorConstants.CREATE_MENU + "Settings/UnityPurchasing",
		fileName = "IAP_Settings_UnityPurchasing")]
	public class UnityPurchasingSettingsScriptableObject : SingleContentEntryScriptableObject<UnityPurchasingSettings>
	{
		[Space, DictionaryDrawerSettings(KeyLabel = "Store", ValueLabel = "Scheme")]
		public SerializableDictionary<DistributionEntry, SerializableBillingScheme> storeToScheme;

		protected override void OnImport(ref UnityPurchasingSettings settings)
		{
			settings.storeToScheme = new();
			foreach (var (store, scheme) in storeToScheme)
				settings.storeToScheme[store] = scheme;
		}
	}

	[Serializable]
	public struct SerializableBillingScheme
	{
		public BillingScheme billingScheme;

		[LabelText("Country to Platform")]
		[DictionaryDrawerSettings(KeyLabel = "Country", ValueLabel = "Platform (Billing)")]
		public SerializableDictionary<CountryEntry, IAPBillingEntry> countryToBilling;

		public static implicit operator BillingScheme(SerializableBillingScheme serializable)
		{
			var scheme = serializable.billingScheme;
			scheme.countryToBilling = serializable.countryToBilling;
			return scheme;
		}
	}
}
