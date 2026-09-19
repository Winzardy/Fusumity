using System;
using UnityEngine.Purchasing;

namespace InAppPurchasing.Unity
{
	using UnitySubscriptionInfo = UnityEngine.Purchasing.SubscriptionInfo;
	using UnityProduct = UnityEngine.Purchasing.Product;

	internal static class UnityPurchasingUtility
	{
		private const string FAKE_UNITY_PRICE = "$0.01";

		internal static ProductType ToUnityProductType(this IAPProductEntry entry)
			=> entry.Type switch
			{
				IAPProductType.Consumable => ProductType.Consumable,
				IAPProductType.NonConsumable => ProductType.NonConsumable,
				IAPProductType.Subscription => ProductType.Subscription,
				_ => throw new ArgumentOutOfRangeException()
			};

		internal static bool IsActive(this UnitySubscriptionInfo subscriptionInfo)
			=> subscriptionInfo.IsSubscribed() == Result.True && subscriptionInfo.IsExpired() == Result.False;

		public static bool IsActive(this SubscriptionInfo subscriptionInfo)
			=> subscriptionInfo is {isSubscribed: true, isExpired: false};

		internal static IAPProductType ToProductType(this ProductType type)
			=> type switch
			{
				ProductType.Consumable => IAPProductType.Consumable,
				ProductType.NonConsumable => IAPProductType.NonConsumable,
				ProductType.Subscription => IAPProductType.Subscription,
				_ => throw new ArgumentOutOfRangeException()
			};

		internal static SubscriptionInfo Convert(this UnitySubscriptionInfo unityInfo)
		{
			return new SubscriptionInfo
			(
				unityInfo.GetProductId(),
				unityInfo.IsSubscribed() == Result.True,
				unityInfo.GetSubscriptionPeriod(),
				unityInfo.IsExpired() == Result.True,
				unityInfo.IsCancelled() == Result.True,
				unityInfo.IsFreeTrial() == Result.True,
				unityInfo.GetFreeTrialPeriod(),
				unityInfo.IsAutoRenewing() == Result.True,
				unityInfo.GetPurchaseDate(),
				unityInfo.GetExpireDate(),
				unityInfo.GetCancelDate(),
				unityInfo.GetRemainingTime(),
				unityInfo.IsIntroductoryPricePeriod() == Result.True,
				unityInfo.GetIntroductoryPrice(),
				unityInfo.GetIntroductoryPricePeriod(),
				unityInfo.GetIntroductoryPricePeriodCycles()
			);
		}

		internal static ProductInfo Convert(this UnityProduct product, string fakePrice = null)
		{
			var price = product.metadata.localizedPriceString;

			if (fakePrice != null && price == FAKE_UNITY_PRICE)
				price = fakePrice;

			return new ProductInfo
			(
				product.definition.id,
				product.definition.type.ToProductType(),
				price,
				product.metadata.localizedPrice,
				product.metadata.isoCurrencyCode
			);
		}
	}
}
