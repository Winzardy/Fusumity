using System;
using System.Collections.Generic;
using Sapientia.Extensions;

namespace InAppPurchasing.Unity
{
	using UnitySubscriptionInfo = UnityEngine.Purchasing.SubscriptionInfo;

	public partial class UnityPurchasingService
	{
		private const int DELAY_MS = 10000; //10 secs
		private static readonly long DELAY_TICKS = DELAY_MS.ToTicks();

		private Dictionary<IAPProductEntry, SubscriptionCache> _subscriptionToCache;

		public bool CanPurchaseSubscription(IAPProductEntry product, out IAPPurchaseError? error)
		{
			if (product.Type != IAPProductType.Subscription)
			{
				error = IAPPurchaseErrorCode.InvalidProductType;
				return false;
			}

			var cache = GetSubscriptionCache(product, out var failureReason, false);

			switch (failureReason)
			{
				case SubscriptionUpdateFailureReason.None:
					break;
				case SubscriptionUpdateFailureReason.UnityProductNotFound:
					error = IAPPurchaseErrorCode.ProductNotFoundInService;
					return false;
				case SubscriptionUpdateFailureReason.CannotRetrieveUnitySubscriptionInfo:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (cache.info.IsActive())
			{
				error = IAPPurchaseErrorCode.Purchased;
				return false;
			}

			return CanPurchase(product, out error);
		}

		/// <returns>Возвращает успешность запроса на покупку, а не статус покупки</returns>
		public bool RequestPurchaseSubscription(IAPProductEntry product)
		{
			if (!CanPurchaseSubscription(product, out _))
				return false;

			return RequestPurchase(product);
		}

		/// <summary>
		/// Собирает данные о подписке (Runtime). Важно что это метод именно собирает актуальные данные с платформы.
		/// Он не выдает кеш или что-то в этом роде!
		/// </summary>
		public ref readonly SubscriptionInfo GetSubscriptionInfo(IAPSubscriptionProductEntry subscription, bool force = false)
			=> ref GetSubscriptionCache(subscription, out _, force).info;

		private SubscriptionCache GetSubscriptionCache(IAPProductEntry product, out SubscriptionUpdateFailureReason failureReason,
			bool force)
		{
			failureReason = SubscriptionUpdateFailureReason.None;

			_subscriptionToCache ??= new Dictionary<IAPProductEntry, SubscriptionCache>(2);

			if (_subscriptionToCache.TryGetValue(product, out var cache))
			{
				if (!force && DateTime.Now.Ticks - cache.timeTicks < DELAY_TICKS)
					return cache;
			}
			else
			{
				_subscriptionToCache[product] = new();
			}

			if (!UpdateInfo(product, out var reason))
			{
				IAPDebug.LogError("Failed to collect subscription info: " + reason);
			}

			return _subscriptionToCache[product];
		}

		private bool UpdateInfo(IAPProductEntry product, out SubscriptionUpdateFailureReason reason)
		{
			reason = SubscriptionUpdateFailureReason.None;

			if (!TryGetUnityProduct(product, out var unityProduct))
			{
				reason = SubscriptionUpdateFailureReason.UnityProductNotFound;
				return false;
			}

			if (!unityProduct.TryGetUnitySubscriptionInfo(out var unityInfo, true))
			{
				reason = SubscriptionUpdateFailureReason.CannotRetrieveUnitySubscriptionInfo;
				return false;
			}

			var cache = _subscriptionToCache[product];
			cache.timeTicks = DateTime.Now.Ticks;
			cache.info = unityInfo.Convert();
			cache.rawInfo = unityInfo;

			return true;
		}
	}

	internal enum SubscriptionUpdateFailureReason
	{
		None,

		UnityProductNotFound,
		CannotRetrieveUnitySubscriptionInfo
	}

	internal class SubscriptionCache
	{
		public long timeTicks;
		public SubscriptionInfo info;

		public UnitySubscriptionInfo rawInfo;
	}
}
