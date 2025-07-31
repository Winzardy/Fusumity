using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes.Odin;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using ShowIfAttribute = Fusumity.Attributes.Specific.ShowIfAttribute;

namespace InAppPurchasing.Unity.Editor
{
	public class UnityPurchasingSettingsAttributeProcessor : OdinAttributeProcessor<UnityPurchasingSettings>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));

			var appStoreGroup = IAPBillingUtility.GetLabel(IAPBillingType.APP_STORE);
			var googlePlayGroup = IAPBillingUtility.GetLabel(IAPBillingType.GOOGLE_PLAY);

			switch (member.Name)
			{
				case nameof(UnityPurchasingSettings.appleSimulateAskToBuy):
					attributes.Add(new TitleGroupAttribute(appStoreGroup));

					attributes.Add(new LabelTextAttribute("Simulate Ask To Buy"));
					break;

				case nameof(UnityPurchasingSettings.appleDisableValidationRecipe):
					attributes.Add(new TitleGroupAttribute(appStoreGroup));

					attributes.Add(new DisableIfAttribute(nameof(UnityPurchasingSettings.disableValidationRecipe)));
					attributes.Add(new LabelTextAttribute(nameof(UnityPurchasingSettings.disableValidationRecipe), true));
					break;

				case nameof(UnityPurchasingSettings.applePromotionalContinuePurchase):
					attributes.Add(new TitleGroupAttribute(appStoreGroup));

					attributes.Add(new LabelTextAttribute("Promotional Continue Purchase"));
					break;

				case nameof(UnityPurchasingSettings.applePromotionalContinueDelayMs):
					attributes.Add(new TitleGroupAttribute(appStoreGroup));

					attributes.Add(new ShowIfAttribute(nameof(UnityPurchasingSettings.applePromotionalContinuePurchase)));
					attributes.Add(new LabelTextAttribute("Delay", true));
					attributes.Add(new UnitAttribute(Units.Millisecond));
					attributes.Add(new MinimumAttribute(0));
					attributes.Add(new IndentAttribute());
					break;

				case nameof(UnityPurchasingSettings.googlePlayDisableValidationRecipe):
					attributes.Add(new TitleGroupAttribute(googlePlayGroup));

					attributes.Add(new DisableIfAttribute(nameof(UnityPurchasingSettings.disableValidationRecipe)));
					attributes.Add(new LabelTextAttribute(nameof(UnityPurchasingSettings.disableValidationRecipe), true));
					break;
			}
		}
	}
}
