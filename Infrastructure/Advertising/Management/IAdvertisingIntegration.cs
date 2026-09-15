namespace Advertising
{
	public struct ShowRewardedArgs
	{
		public AdPlacementEntry placement;
		public bool disableAutoLoad;

		/// <summary>
		/// Передавать ли в интеграцию placement
		/// </summary>
		public bool track;
	}

	public struct ShowInterstitialArgs
	{
		public AdPlacementEntry placement;
		public bool disableAutoLoad;

		/// <summary>
		/// Передавать ли в интеграцию placement
		/// </summary>
		public bool track;
	}

	public interface IAdvertisingIntegration : IAdIntegrationEvents
	{
		string Name { get; }

		#region Rewarded

		bool CanShowRewarded(AdPlacementEntry placement, out AdShowError? error);

		/// <returns>Успешность запроса</returns>
		bool ShowRewarded(in ShowRewardedArgs args);

		bool LoadRewarded(AdPlacementEntry placement);

		AdLoadingStatus GetRewardedLoadingStatus(AdPlacementEntry placement);

		#endregion

		#region Interstitial

		bool CanShowInterstitial(AdPlacementEntry placement, out AdShowError? error);

		/// <returns>Успешность запроса</returns>
		bool ShowInterstitial(in ShowInterstitialArgs args);

		bool LoadInterstitial(AdPlacementEntry placement);

		AdLoadingStatus GetInterstitialLoadingStatus(AdPlacementEntry placement);

		#endregion
	}

	public enum AdLoadingStatus
	{
		None,
		Loading,
		Loaded
	}

	public interface IAdIntegrationEvents
	{
		/// <summary>
		/// Доход за отдельный показ, событие вызывается в главном потоке
		/// </summary>
		event AdRevenuePaid AdRevenuePaid;

		#region Rewarded

		event RewardedClicked RewardedClicked;

		event RewardedClosed RewardedClosed;
		event RewardedDisplayed RewardedDisplayed;
		event RewardedDisplayFailed RewardedDisplayFailed;
		event RewardedLoaded RewardedLoaded;
		event RewardedLoadFailed RewardedLoadFailed;
		event RewardedCompleted RewardedCompleted;

		#endregion

		#region Interstitial

		event InterstitialClicked InterstitialClicked;

		event InterstitialClosed InterstitialClosed;
		event InterstitialDisplayed InterstitialDisplayed;
		event InterstitialDisplayFailed InterstitialDisplayFailed;
		event InterstitialLoaded InterstitialLoaded;
		event InterstitialLoadFailed InterstitialLoadFailed;

		#endregion
	}

	public interface IAdEvents : IAdIntegrationEvents
	{
		event AdDisplayStarted AdDisplayStarted;
		event AdDisplayFinished AdDisplayFinished;
	}

	/// <summary>
	/// Доход за один показ без типов рекламного SDK или системы аналитики
	/// </summary>
	public struct AdRevenueData
	{
		/// <summary>
		/// Сеть, показавшая рекламу, может отличаться от используемого медиатора
		/// </summary>
		public string network;
		public AdMediation mediation;
		public double revenue;

		/// <summary>
		/// Код валюты по ISO 4217
		/// </summary>
		public string currency;

		public string country;
		public string adUnitId;
		public string adFormat;
		public string placement;
	}

	public enum AdMediation
	{
		Custom,
		UnityLevelPlay,
		GoogleAdMob,
		AppLovinMax,
		Direct
	}

	public delegate void AdRevenuePaid(in AdRevenueData data);

	#region Rewarded Delegates

	public delegate void RewardedClicked(AdPlacementEntry placement, object rawData = null);

	/// <summary>
	/// Rewarded был полностью просмотрен, а не просто показан, как в случае <see cref="RewardedClosed"/>
	/// </summary>
	public delegate void RewardedCompleted(AdPlacementEntry placement, object rawData = null);

	public delegate void RewardedLoaded(object rawData = null);

	public delegate void RewardedLoadFailed(string error, object rawData = null);

	/// <summary>
	/// Начали показывать
	/// </summary>
	public delegate void RewardedDisplayed(AdPlacementEntry placement, object rawData = null);

	public delegate void RewardedDisplayFailed(AdPlacementEntry placement, string error, object rawData = null);

	/// <summary>
	/// Закончился показ, есть отдельное событие которое говорит что полностью 'показали' <see cref="RewardedCompleted"/>
	/// </summary>
	public delegate void RewardedClosed(AdPlacementEntry placement, bool full, object rawData = null);

	#endregion

	#region Interstitial Delegates

	public delegate void InterstitialClicked(AdPlacementEntry placement, object rawData = null);

	public delegate void InterstitialClosed(AdPlacementEntry placement, object rawData = null);

	public delegate void InterstitialLoaded(object rawData = null);

	public delegate void InterstitialLoadFailed(string error, object rawData = null);

	public delegate void InterstitialDisplayed(AdPlacementEntry placement, object rawData = null);

	public delegate void InterstitialDisplayFailed(AdPlacementEntry placement, string error, object rawData = null);

	#endregion

	#region Extended Delegates

	public delegate void AdDisplayStarted(AdPlacementEntry placement);
	public delegate void AdDisplayFinished(AdPlacementEntry placement, bool full );

	#endregion

}
