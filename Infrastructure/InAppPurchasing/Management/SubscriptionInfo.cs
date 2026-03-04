using System;
using Sapientia.Extensions;

namespace InAppPurchasing
{
	public readonly struct SubscriptionInfo
	{
		public readonly string productId;

		public readonly bool isSubscribed;
		public readonly TimeSpan subscriptionPeriod;

		public readonly bool isExpired;
		public readonly bool isCancelled;

		public readonly bool isFreeTrial;
		public readonly TimeSpan freeTrialPeriod;

		public readonly bool isAutoRenewing;

		public readonly DateTime purchaseDate;
		public readonly DateTime subscriptionExpireDate;
		public readonly DateTime subscriptionCancelDate;
		public readonly TimeSpan remainedTime;

		public readonly bool isIntroductoryPricePeriod;
		public readonly string introductoryPrice;
		public readonly TimeSpan introductoryPricePeriod;
		public readonly long introductoryPricePeriodCycles;

		public SubscriptionInfo(
			string productId, bool isSubscribed, TimeSpan subscriptionPeriod, bool isExpired, bool isCancelled,
			bool isFreeTrial, TimeSpan freeTrialPeriod, bool isAutoRenewing, DateTime purchaseDate,
			DateTime subscriptionExpireDate, DateTime subscriptionCancelDate, TimeSpan remainedTime,
			bool isIntroductoryPricePeriod, string introductoryPrice, TimeSpan introductoryPricePeriod,
			long introductoryPricePeriodCycles)
		{
			this.productId = productId;
			this.isSubscribed = isSubscribed;
			this.subscriptionPeriod = subscriptionPeriod;
			this.isExpired = isExpired;
			this.isCancelled = isCancelled;
			this.isFreeTrial = isFreeTrial;
			this.freeTrialPeriod = freeTrialPeriod;
			this.isAutoRenewing = isAutoRenewing;
			this.purchaseDate = purchaseDate;
			this.subscriptionExpireDate = subscriptionExpireDate;
			this.subscriptionCancelDate = subscriptionCancelDate;
			this.remainedTime = remainedTime;
			this.isIntroductoryPricePeriod = isIntroductoryPricePeriod;
			this.introductoryPrice = introductoryPrice;
			this.introductoryPricePeriod = introductoryPricePeriod;
			this.introductoryPricePeriodCycles = introductoryPricePeriodCycles;
		}

		public static implicit operator bool(SubscriptionInfo info) => !info.productId.IsNullOrEmpty();

		public override string ToString()
		{
			return "SubscriptionInfo:\n" +
				$"  Product ID: {productId}\n" +
				$"  Is Subscribed: {isSubscribed}\n" +
				$"  Subscription Period: {subscriptionPeriod}\n" +
				$"  Is Expired: {isExpired}\n" +
				$"  Is Cancelled: {isCancelled}\n" +
				$"  Is Free Trial: {isFreeTrial}\n" +
				$"  Free Trial Period: {freeTrialPeriod}\n" +
				$"  Is Auto-Renewing: {isAutoRenewing}\n" +
				$"  Purchase Date: {purchaseDate:yyyy-MM-dd HH:mm:ss}\n" +
				$"  Subscription Expire Date: {subscriptionExpireDate:yyyy-MM-dd HH:mm:ss}\n" +
				$"  Subscription Cancel Date: {(subscriptionCancelDate == default ? "N/A" : subscriptionCancelDate.ToString("yyyy-MM-dd HH:mm:ss"))}\n" +
				$"  Remaining Time: {remainedTime}\n" +
				$"  Is Introductory Price Period: {isIntroductoryPricePeriod}\n" +
				$"  Introductory Price: {introductoryPrice}\n" +
				$"  Introductory Price Period: {introductoryPricePeriod}\n" +
				$"  Introductory Price Period Cycles: {introductoryPricePeriodCycles}";
		}
	}
}
