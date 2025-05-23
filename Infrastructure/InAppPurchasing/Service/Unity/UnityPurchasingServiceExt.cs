using System;
using Sapientia.Extensions;
using UnityEngine.Purchasing;
using UnitySubscriptionInfo = UnityEngine.Purchasing.SubscriptionInfo;

namespace InAppPurchasing.Unity
{
	internal static class UnityPurchasingServiceExt
	{
		//Костыль чтобы не тянуть его через всю цепочку
		internal static IAppleExtensions appleExtensions;

		internal static ProductType ToUnityProductType(this IAPProductEntry entry)
			=> entry.Type switch
			{
				IAPProductType.Consumable => ProductType.Consumable,
				IAPProductType.NonConsumable => ProductType.NonConsumable,
				IAPProductType.Subscription => ProductType.Subscription,
				_ => throw new ArgumentOutOfRangeException()
			};

		internal static bool IsPurchased(this Product product) => IsPurchased(product, out _);

		internal static bool IsPurchased(this Product product, out object rawData)
		{
			rawData = null;
			if (product.definition.type == ProductType.Consumable)
			{
				rawData = product;
				return product.hasReceipt;
			}

			if (product.TryGetUnitySubscriptionInfo(out var info))
			{
				rawData = info;
				return info.IsActive();
			}

			return false;
		}

		private static bool IsActive(this UnitySubscriptionInfo subscriptionInfo)
			=> subscriptionInfo.isSubscribed() == Result.True && subscriptionInfo.isExpired() == Result.False;

		public static bool IsActive(this SubscriptionInfo subscriptionInfo)
			=> subscriptionInfo is {isSubscribed: true, isExpired: false};

		internal static bool TryGetUnitySubscriptionInfo(this Product product, out UnityEngine.Purchasing.SubscriptionInfo info,
			bool debug = false)
		{
			const string PREFIX_DEBUG = "Couldn't get subscription info:";

			info = null;

			var storeProductId = product.definition.id;

			if (product.definition.type != ProductType.Subscription)
			{
				if (debug)
					IAPDebug.Log($"{PREFIX_DEBUG} this product is not a subscription by store product id  [ {storeProductId} ]");
				return false;
			}

			if (product.receipt.IsNullOrEmpty())
			{
				if (debug)
					IAPDebug.Log($"{PREFIX_DEBUG} this product doesn't have a valid receipt by store product id [ {storeProductId} ]");
				return false;
			}

			TryGetSubscriptionIntroJson(product, out var introJson);
			var subscriptionManager = new SubscriptionManager(product, introJson);
			info = subscriptionManager.getSubscriptionInfo();
			return true;
		}

		private static bool TryGetSubscriptionIntroJson(Product product, out string json)
		{
			json = null;

			if (appleExtensions == null)
				return false;

			var prices = appleExtensions.GetIntroductoryPriceDictionary();
			json = (prices == null || !prices.TryGetValue(product.definition.storeSpecificId, out var price))
				? null
				: price;
			return true;
		}

		internal static IAPProductType ToProductType(this ProductType type)
			=> type switch
			{
				ProductType.Consumable => IAPProductType.Consumable,
				ProductType.NonConsumable => IAPProductType.NonConsumable,
				ProductType.Subscription => IAPProductType.Subscription,
				_ => throw new ArgumentOutOfRangeException()
			};

		internal static UnityPurchasingInitializationFailureReason Convert(this InitializationFailureReason reason)
			=> reason switch
			{
				InitializationFailureReason.PurchasingUnavailable => UnityPurchasingInitializationFailureReason.PurchasingUnavailable,
				InitializationFailureReason.NoProductsAvailable => UnityPurchasingInitializationFailureReason.NoProductsAvailable,
				InitializationFailureReason.AppNotKnown => UnityPurchasingInitializationFailureReason.AppNotKnown,
				_ => throw new ArgumentOutOfRangeException()
			};

		internal static SubscriptionInfo Convert(this UnitySubscriptionInfo unityInfo)
		{
			return new SubscriptionInfo
			(
				unityInfo.getProductId(),
				unityInfo.isSubscribed() == Result.True,
				unityInfo.getSubscriptionPeriod(),
				unityInfo.isExpired() == Result.True,
				unityInfo.isCancelled() == Result.True,
				unityInfo.isFreeTrial() == Result.True,
				unityInfo.getFreeTrialPeriod(),
				unityInfo.isAutoRenewing() == Result.True,
				unityInfo.getPurchaseDate(),
				unityInfo.getExpireDate(),
				unityInfo.getCancelDate(),
				unityInfo.getRemainingTime(),
				unityInfo.isIntroductoryPricePeriod() == Result.True,
				unityInfo.getIntroductoryPrice(),
				unityInfo.getIntroductoryPricePeriod(),
				unityInfo.getIntroductoryPricePeriodCycles()
			);
		}
	}
}
