using Sapientia;

namespace Advertising
{
	internal class AdvertisingIntegrationRelay : Relay<IAdvertisingIntegration>, IAdEvents
	{
		public event RewardedClicked RewardedClicked;
		public event RewardedClosed RewardedClosed;
		public event RewardedDisplayed RewardedDisplayed;
		public event RewardedDisplayFailed RewardedDisplayFailed;
		public event RewardedLoaded RewardedLoaded;
		public event RewardedLoadFailed RewardedLoadFailed;
		public event RewardedCompleted RewardedCompleted;

		public event InterstitialClicked InterstitialClicked;
		public event InterstitialClosed InterstitialClosed;
		public event InterstitialDisplayed InterstitialDisplayed;
		public event InterstitialDisplayFailed InterstitialDisplayFailed;
		public event InterstitialLoaded InterstitialLoaded;
		public event InterstitialLoadFailed InterstitialLoadFailed;

		public event AdDisplayStarted AdDisplayStarted;
		public event AdDisplayFinished AdDisplayFinished;

		protected override void OnBind(IAdvertisingIntegration integration)
		{
			integration.RewardedClicked += OnRewardedClicked;
			integration.RewardedClosed += OnRewardedClosed;
			integration.RewardedDisplayed += OnRewardedDisplayed;
			integration.RewardedDisplayFailed += OnRewardedDisplayFailed;
			integration.RewardedLoaded += OnRewardedLoaded;
			integration.RewardedLoadFailed += OnRewardedLoadFailed;
			integration.RewardedCompleted += OnRewardedCompleted;

			integration.InterstitialClicked += OnInterstitialClicked;
			integration.InterstitialClosed += OnInterstitialClosed;
			integration.InterstitialDisplayed += OnInterstitialDisplayed;
			integration.InterstitialDisplayFailed += OnInterstitialDisplayFailed;
			integration.InterstitialLoaded += OnInterstitialLoaded;
			integration.InterstitialLoadFailed += OnInterstitialLoadFailed;
		}

		protected override void OnClear(IAdvertisingIntegration integration)
		{
			integration.RewardedClicked -= OnRewardedClicked;
			integration.RewardedClosed -= OnRewardedClosed;
			integration.RewardedDisplayed -= OnRewardedDisplayed;
			integration.RewardedDisplayFailed -= OnRewardedDisplayFailed;
			integration.RewardedLoaded -= OnRewardedLoaded;
			integration.RewardedLoadFailed -= OnRewardedLoadFailed;
			integration.RewardedCompleted -= OnRewardedCompleted;

			integration.InterstitialClicked -= OnInterstitialClicked;
			integration.InterstitialClosed -= OnInterstitialClosed;
			integration.InterstitialDisplayed -= OnInterstitialDisplayed;
			integration.InterstitialDisplayFailed -= OnInterstitialDisplayFailed;
			integration.InterstitialLoaded -= OnInterstitialLoaded;
			integration.InterstitialLoadFailed -= OnInterstitialLoadFailed;
		}

		private void OnRewardedClicked(AdPlacementEntry placement, object rawData)
			=> RewardedClicked?.Invoke(placement, rawData);

		private void OnRewardedClosed(AdPlacementEntry placement, bool full, object rawData)
		{
			RewardedClosed?.Invoke(placement, full, rawData);
			OnAdFinished(placement); // ⏹
		}

		private void OnRewardedDisplayed(AdPlacementEntry placement, object rawData)
		{
			RewardedDisplayed?.Invoke(placement, rawData);
			OnAdStarted(placement); // ▶
		}

		private void OnRewardedDisplayFailed(AdPlacementEntry placement, string error, object rawData)
		{
			RewardedDisplayFailed?.Invoke(placement, error, rawData);
			OnAdFinished(placement); // ⏹
		}

		private void OnRewardedLoaded(object rawData) => RewardedLoaded?.Invoke(rawData);
		private void OnRewardedLoadFailed(string error, object rawData) => RewardedLoadFailed?.Invoke(error, rawData);
		private void OnRewardedCompleted(AdPlacementEntry placement, object rawData) => RewardedCompleted?.Invoke(placement, rawData);

		private void OnInterstitialClicked(AdPlacementEntry placement, object rawData) => InterstitialClicked?.Invoke(placement, rawData);

		private void OnInterstitialClosed(AdPlacementEntry placement, object rawData)
		{
			InterstitialClosed?.Invoke(placement, rawData);
			OnAdFinished(placement); // ▶
		}

		private void OnInterstitialDisplayed(AdPlacementEntry placement, object rawData)
		{
			InterstitialDisplayed?.Invoke(placement, rawData);
			OnAdStarted(placement); // ⏹
		}

		private void OnInterstitialDisplayFailed(AdPlacementEntry placement, string error, object rawData)
		{
			InterstitialDisplayFailed?.Invoke(placement, error, rawData);
			OnAdFinished(placement); // ▶
		}

		private void OnInterstitialLoaded(object rawData) => InterstitialLoaded?.Invoke(rawData);
		private void OnInterstitialLoadFailed(string error, object rawData) => InterstitialLoadFailed?.Invoke(error, rawData);

		private void OnAdStarted(AdPlacementEntry placement) // ⏹
		{
			AdsDebug.Log($"[{placement.Type}] [ {placement.Id} ] display has begun");
			AdDisplayStarted?.Invoke(placement);
		}

		private void OnAdFinished(AdPlacementEntry placement, bool full = true) // ▶
		{
			var postfix = string.Empty;
			if (!full)
				postfix = " (not full)";
			AdsDebug.Log($"[{placement.Type}] [ {placement.Id} ] display has ended{postfix}");
			AdDisplayFinished?.Invoke(placement, full);
		}
	}
}
