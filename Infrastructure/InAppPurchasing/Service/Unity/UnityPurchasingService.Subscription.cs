using System;
using System.Collections.Generic;
using Sapientia.Extensions;

namespace InAppPurchasing.Unity
{
	using UnitySubscriptionInfo = UnityEngine.Purchasing.SubscriptionInfo;


	public partial class UnityPurchasingService
	{
		private const int DELAY_UPDATING_SUBSCRIPTION_CACHE_MS = 10000; // 10 secs
		private static readonly long DELAY_UPDATING_SUBSCRIPTION_CACHE_TICKS = DELAY_UPDATING_SUBSCRIPTION_CACHE_MS.ToTicks();

		private Dictionary<IAPProductEntry, SubscriptionCache> _subscriptionToCache;

		public bool CanPurchaseSubscription(IAPProductEntry entry, out IAPPurchaseError? error)
		{
			if (entry.Type != IAPProductType.Subscription)
			{
				error = IAPPurchaseErrorCode.InvalidProductType;
				return false;
			}

			var cache = GetSubscriptionCache(entry, out var failureReason, false);

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

			return CanPurchase(entry, out error);
		}

		/// <returns>Возвращает успешность запроса на покупку, а не статус покупки</returns>
		public bool RequestPurchaseSubscription(IAPProductEntry entry)
		{
			if (!CanPurchaseSubscription(entry, out _))
				return false;

			return RequestPurchase(entry);
		}

		#region Subscription Info

		/// <summary>
		/// Получить актуальную информацию о подписке с платформы
		/// </summary>
		/// <param name="forceUpdateCache">при <c>true</c> форсит обновление кеша</param>
		/// <remarks>
		/// От частых запросов к платформе, возвращает кэшированную информацию,
		/// можно зафорсировать обновление кеша.
		/// Кеш хранится <c>10 секунд</c> (<see cref="DELAY_MS"/>)
		/// </remarks>
		public ref readonly SubscriptionInfo GetSubscriptionInfo(IAPSubscriptionProductEntry subscription, bool forceUpdateCache = false)
			=> ref GetSubscriptionCache(subscription, out _, forceUpdateCache).info;

		private SubscriptionCache GetSubscriptionCache(IAPProductEntry product, out SubscriptionUpdateFailureReason failureReason,
			bool force)
		{
			failureReason = SubscriptionUpdateFailureReason.None;

			_subscriptionToCache ??= new Dictionary<IAPProductEntry, SubscriptionCache>(2);

			if (_subscriptionToCache.TryGetValue(product, out var cache))
			{
				if (!force && DateTime.Now.Ticks - cache.timeTicks < DELAY_UPDATING_SUBSCRIPTION_CACHE_TICKS)
					return cache;
			}
			else
			{
				_subscriptionToCache[product] = new();
			}

			if (!UpdateInfo(product, out failureReason))
			{
				IAPDebug.LogError("Failed to collect subscription info: " + failureReason);
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

		#endregion
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
