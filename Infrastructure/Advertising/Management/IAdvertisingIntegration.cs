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

		public bool CanShowRewarded(AdPlacementEntry placement, out AdShowError? error);

		/// <returns>Успешность запроса</returns>
		public bool ShowRewarded(in ShowRewardedArgs args);

		public bool LoadRewarded(AdPlacementEntry placement);

		public AdLoadingStatus GetRewardedLoadingStatus(AdPlacementEntry placement);

		#endregion

		#region Interstitial

		public bool CanShowInterstitial(AdPlacementEntry placement, out AdShowError? error);

		/// <returns>Успешность запроса</returns>
		public bool ShowInterstitial(in ShowInterstitialArgs args);

		public bool LoadInterstitial(AdPlacementEntry placement);

		public AdLoadingStatus GetInterstitialLoadingStatus(AdPlacementEntry placement);

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
		#region Rewarded

		public event RewardedClicked RewardedClicked;

		public event RewardedClosed RewardedClosed;
		public event RewardedDisplayed RewardedDisplayed;
		public event RewardedDisplayFailed RewardedDisplayFailed;
		public event RewardedLoaded RewardedLoaded;
		public event RewardedLoadFailed RewardedLoadFailed;
		public event RewardedCompleted RewardedCompleted;

		#endregion

		#region Interstitial

		public event InterstitialClicked InterstitialClicked;

		public event InterstitialClosed InterstitialClosed;
		public event InterstitialDisplayed InterstitialDisplayed;
		public event InterstitialDisplayFailed InterstitialDisplayFailed;
		public event InterstitialLoaded InterstitialLoaded;
		public event InterstitialLoadFailed InterstitialLoadFailed;

		#endregion
	}

	public interface IAdEvents : IAdIntegrationEvents
	{
		public event AdDisplayStarted AdDisplayStarted;
		public event AdDisplayFinished AdDisplayFinished;
	}

	public enum AdPlacementType
	{
		Rewarded, //обычно это видео, но сейчас это могут быть еще и мини игры
		Interstitial,

		//Не стал поддерживать, можно добавить в будущем если это необходимо
		//Banner
		//Native
	}

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
