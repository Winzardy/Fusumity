using System;
using Content;
using Targeting;
using Fusumity.Reactive;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Unity.Services.LevelPlay;
//Юнитеки шизу поймали...
using LevelPlayAdFormat = com.unity3d.mediation.LevelPlayAdFormat;
using LevelPlayAdInfo = com.unity3d.mediation.LevelPlayAdInfo;
using LevelPlayConfiguration = com.unity3d.mediation.LevelPlayConfiguration;
using LevelPlayInitError = com.unity3d.mediation.LevelPlayInitError;
using LevelPlayReward = com.unity3d.mediation.LevelPlayReward;
using LevelPlayAdDisplayInfoError = com.unity3d.mediation.LevelPlayAdDisplayInfoError;
using LevelPlayAdError = com.unity3d.mediation.LevelPlayAdError;

namespace Advertising.UnityLevelPlay
{
	public class UnityLevelPlayAdIntegration : IAdvertisingIntegration, IDisposable
	{
		private readonly UnityLevelPlaySettings _settings;
		private readonly PlatformEntry _platform;

		private readonly UnityLevelPlayPlatformEntry _entry;

		private (LevelPlayRewardedAd ad, AdLoadingStatus status) _rewarded;
		private (LevelPlayInterstitialAd ad, AdLoadingStatus status) _interstitial;

		private bool _initialized;

		private AdPlacementEntry _cacheRewardedPlacement;
		private AdPlacementEntry _cacheInterstitialPlacement;

		public string Name => "UnityLevelPlay";

		public UnityLevelPlayAdIntegration(UnityLevelPlaySettings settings, in PlatformEntry platform)
		{
			_platform = platform;
			_settings = settings;

			UnityLevelPlaySupportedPlatform? unityLevelPlayPlatform =
#if UNITY_ANDROID
				UnityLevelPlaySupportedPlatform.Android;
#elif UNITY_IOS
				UnityLevelPlaySupportedPlatform.IOS;
#else
				null;
#endif
			if (!unityLevelPlayPlatform.HasValue)
			{
				AdsDebug.LogWarning("Unexpected platform");
				return;
			}

			if (!settings.platformToEntry.TryGetValue(unityLevelPlayPlatform.Value, out _entry))
			{
				AdsDebug.LogWarning($"Failed to initialize: Not found app key for platform [ {unityLevelPlayPlatform.Value} ]");
				return;
			}

			if (_entry.appKey.IsNullOrEmpty())
			{
				AdsDebug.LogError($"Failed to initialize: Empty app key for platform [ {unityLevelPlayPlatform.Value} ]");
				return;
			}

			using (ListPool<LevelPlayAdFormat>.Get(out var formats))
			{
				if (ContentManager.Any<RewardedAdPlacementEntry>())
					formats.Add(LevelPlayAdFormat.REWARDED);

				if (ContentManager.Any<InterstitialAdPlacementEntry>())
					formats.Add(LevelPlayAdFormat.INTERSTITIAL);

				if (formats.IsEmpty())
				{
					AdsDebug.LogWarning($"Failed to initialize: Not found any placements");
					return;
				}

				var formatsStr = formats.GetCompositeString(false, numerate: false, separator: ",");

				var debug = false;
#if DebugLog
				debug = true;
				IronSource.Agent.setAdaptersDebug(true);
				IronSource.Agent.setMetaData("is_test_suite", "enable");
#endif
				AdsDebug.Log($"Started to initialize with app key [ {_entry.appKey} ], formats: [ {formatsStr} ], debug: [ {debug} ]");

				IronSource.Agent.setManualLoadRewardedVideo(_settings.manualLoadRewardedVideo);
				IronSource.Agent.shouldTrackNetworkState(!_settings.disableTrackNetworkState);

				//TODO: ConsentManager
				IronSource.Agent.setConsent(true);

				LevelPlay.Init(_entry.appKey, adFormats: formats.ToArray());

				LevelPlay.OnInitSuccess += OnInitialized;
				LevelPlay.OnInitFailed += OnInitializeFailed;
			}
		}

		private void OnInitialized(LevelPlayConfiguration configuration)
		{
			_initialized = true;

			AdsDebug.Log($"Unity LevelPlay successfully initialized: IsAdQualityEnabled [ {configuration.IsAdQualityEnabled} ]");

			IronSourceEvents.onImpressionDataReadyEvent += OnImpressionDataReadyEvent;

			if (!_entry.rewardAdUnitId.IsNullOrEmpty())
			{
				var unit = new LevelPlayRewardedAd(_entry.rewardAdUnitId);

				unit.OnAdClicked += OnRewardedAdClicked;
				unit.OnAdRewarded += OnRewardedCompleted;
				unit.OnAdClosed += OnRewardedClosed;
				unit.OnAdLoaded += OnRewardedLoaded;
				unit.OnAdDisplayed += OnRewardedDisplayed;
				unit.OnAdDisplayFailed += OnRewardedDisplayFailed;
				unit.OnAdLoadFailed += OnRewardedLoadFailed;

				var ready = unit.IsAdReady();
				_rewarded = (unit, ready ? AdLoadingStatus.Loaded : AdLoadingStatus.None);

				if (!ready)
					LoadRewardedInternal();
			}
			else
			{
				AdsDebug.LogError("Failed to initialize: Reward AdUnitId is empty!");
			}

			if (!_entry.interstitialAdUnitId.IsNullOrEmpty())
			{
				var unit = new LevelPlayInterstitialAd(_entry.interstitialAdUnitId);

				unit.OnAdClicked += OnInterstitialClicked;
				unit.OnAdClosed += OnInterstitialClosed;
				unit.OnAdLoaded += OnInterstitialLoaded;
				unit.OnAdDisplayed += OnInterstitialDisplayed;
				unit.OnAdDisplayFailed += OnInterstitialDisplayFailed;
				unit.OnAdLoadFailed += OnInterstitialLoadFailed;

				var ready = unit.IsAdReady();
				_interstitial = (unit, ready ? AdLoadingStatus.Loaded : AdLoadingStatus.None);

				if (!ready)
					LoadInterstitialInternal();
			}
			else
			{
				AdsDebug.LogError("Failed to initialize: Interstitial AdUnitId is empty!");
			}

			ProjectDesk.UserId.Subscribe(OnUserIdChanged);

			SetPause(UnityLifecycle.ApplicationPause);
			UnityLifecycle.ApplicationPauseEvent += OnApplicationPause;
			UnityLifecycle.ApplicationResumeEvent += OnApplicationResume;
		}

		public void Dispose()
		{
			LevelPlay.OnInitSuccess -= OnInitialized;
			LevelPlay.OnInitFailed -= OnInitializeFailed;

			if (!_initialized)
				return;

			IronSourceEvents.onImpressionDataReadyEvent -= OnImpressionDataReadyEvent;
			if (_rewarded.ad != null)
			{
				_rewarded.ad.OnAdClicked -= OnRewardedAdClicked;
				_rewarded.ad.OnAdRewarded -= OnRewardedCompleted;
				_rewarded.ad.OnAdClosed -= OnRewardedClosed;
				_rewarded.ad.OnAdLoaded -= OnRewardedLoaded;
				_rewarded.ad.OnAdDisplayed -= OnRewardedDisplayed;
				_rewarded.ad.OnAdDisplayFailed -= OnRewardedDisplayFailed;
				_rewarded.ad.OnAdLoadFailed -= OnRewardedLoadFailed;

				_rewarded.ad.Dispose(); //Так же есть DestroyAd(), но он по вызывает внутри Dispose(), шизики
			}

			if (_interstitial.ad != null)
			{
				_interstitial.ad.OnAdClicked -= OnInterstitialClicked;
				_interstitial.ad.OnAdClosed -= OnInterstitialClosed;
				_interstitial.ad.OnAdLoaded -= OnInterstitialLoaded;
				_interstitial.ad.OnAdDisplayed -= OnInterstitialDisplayed;
				_interstitial.ad.OnAdDisplayFailed -= OnInterstitialDisplayFailed;
				_interstitial.ad.OnAdLoadFailed -= OnInterstitialLoadFailed;

				_interstitial.ad.Dispose();
			}

			ProjectDesk.UserId.Unsubscribe(OnUserIdChanged);

			UnityLifecycle.ApplicationPauseEvent -= OnApplicationPause;
			UnityLifecycle.ApplicationResumeEvent -= OnApplicationResume;
		}

		private void OnInitializeFailed(LevelPlayInitError error)
			=> AdsDebug.LogError($"Failed to initialize: {error.ErrorCode}, {error.ErrorMessage}");

		private void OnImpressionDataReadyEvent(IronSourceImpressionData data)
			=> AdsDebug.Log("Unity LevelPlay Impression Data Ready data: " + data.allData);

		#region Rewarded

		public event RewardedClicked RewardedClicked;
		public event RewardedCompleted RewardedCompleted;
		public event RewardedClosed RewardedClosed;
		public event RewardedDisplayed RewardedDisplayed;
		public event RewardedDisplayFailed RewardedDisplayFailed;
		public event RewardedLoaded RewardedLoaded;
		public event RewardedLoadFailed RewardedLoadFailed;

		public bool CanShowRewarded(AdPlacementEntry _, out AdShowError? error)
		{
			error = null;

			if (_rewarded.ad == null)
			{
				error = AdShowErrorCode.NotInitialized;
				return false;
			}

			if (_rewarded.status != AdLoadingStatus.Loaded)
			{
				error = AdShowErrorCode.NotLoaded;
				return false;
			}

			if (!_rewarded.ad.IsAdReady())
			{
				error = AdShowErrorCode.NotReady;
				return false;
			}

			return true;
		}

		/// <inheritdoc cref="IAdvertisingIntegration.ShowRewarded"/>
		public bool ShowRewarded(in ShowRewardedArgs args)
		{
			var track = args.track;
			_cacheRewardedPlacement = args.placement;

			if (!CanShowRewarded(_cacheRewardedPlacement, out var errorCode))
			{
				#region Auto Load

				//TODO: обрабатывать если запросили другой Placement пока этот грузится!
				if (errorCode == AdShowErrorCode.NotLoaded && !args.disableAutoLoad)
				{
					if (LoadRewarded(_cacheRewardedPlacement))
					{
						_rewarded.ad.OnAdLoadFailed += OnAdLoadFailed;
						_rewarded.ad.OnAdLoaded += OnAdLoaded;

						void OnAdLoaded(LevelPlayAdInfo info)
						{
							_rewarded.ad.OnAdLoaded -= OnAdLoaded;
							_rewarded.ad.OnAdLoadFailed -= OnAdLoadFailed;
							ShowInternal(_cacheRewardedPlacement);
						}

						void OnAdLoadFailed(LevelPlayAdError error)
						{
							_rewarded.ad.OnAdLoaded -= OnAdLoaded;
							_rewarded.ad.OnAdLoadFailed -= OnAdLoadFailed;
							AdsDebug.LogError(error.ToString());
						}
					}

					return true;
				}

				#endregion

				return false;
			}

			ShowInternal(_cacheRewardedPlacement);
			return true;

			void ShowInternal(in AdPlacementEntry placement)
			{
				var name = placement.GetName(_platform);
				_rewarded.ad.ShowAd(track ? name : null);
			}
		}

		/// <inheritdoc cref="LoadRewardedInternal"/>
		public bool LoadRewarded(AdPlacementEntry _)
		{
			if (_rewarded.status == AdLoadingStatus.Loaded)
				return false;

			//Может быть Manual!
			return LoadRewardedInternal();
		}

		/// <returns>Возвращает успешность запроса</returns>
		private bool LoadRewardedInternal()
		{
			if (_interstitial.status == AdLoadingStatus.Loading)
			{
				AdsDebug.Log($"[{AdPlacementType.Rewarded}] already loading...");
				return false;
			}

			_rewarded.ad.LoadAd();
			AdsDebug.Log($"[{AdPlacementType.Rewarded}] initiated loading");
			return true;
		}

		public AdLoadingStatus GetRewardedLoadingStatus(AdPlacementEntry placement) => _rewarded.status;

		private void OnRewardedAdClicked(LevelPlayAdInfo info)
			=> RewardedClicked?.Invoke(_cacheRewardedPlacement, info);

		//Может ли нарушится порядок OnRewardedClosed и OnRewardedCompleted? Пока OnRewardedCompleted всегда раньше
		private void OnRewardedClosed(LevelPlayAdInfo info) => NotifyRewardedClosed(false);

		private void OnRewardedCompleted(LevelPlayAdInfo info, LevelPlayReward _)
		{
			if (!_cacheRewardedPlacement)
				AdsDebug.LogError("Placement is null!");

			RewardedCompleted?.Invoke(_cacheRewardedPlacement, info);
			NotifyRewardedClosed(true);
		}

		private void NotifyRewardedClosed(bool full)
		{
			if (!_cacheRewardedPlacement)
				return;

			SetRewardLoadingStatus(AdLoadingStatus.None);
			RewardedClosed?.Invoke(_cacheRewardedPlacement, full);
			_cacheRewardedPlacement = null;
		}

		private void OnRewardedLoaded(LevelPlayAdInfo info)
		{
			AdsDebug.Log($"[{AdPlacementType.Rewarded}] loaded");
			SetRewardLoadingStatus(AdLoadingStatus.Loaded);
			RewardedLoaded?.Invoke(info);
		}

		private void OnRewardedLoadFailed(LevelPlayAdError error)
		{
			SetRewardLoadingStatus(AdLoadingStatus.None);
			RewardedLoadFailed?.Invoke(error.ToString(), error);
		}

		private void OnRewardedDisplayed(LevelPlayAdInfo info)
			=> RewardedDisplayed?.Invoke(_cacheRewardedPlacement, info);

		private void OnRewardedDisplayFailed(LevelPlayAdDisplayInfoError error)
		{
			SetRewardLoadingStatus(AdLoadingStatus.None);
			RewardedDisplayFailed?.Invoke(_cacheRewardedPlacement, error.ToString(), error);
		}

		private void SetRewardLoadingStatus(AdLoadingStatus status)
		{
			_rewarded.status = status;
		}

		#endregion

		#region Interstitial

		public event InterstitialClicked InterstitialClicked;
		public event InterstitialClosed InterstitialClosed;

		public event InterstitialLoaded InterstitialLoaded;
		public event InterstitialLoadFailed InterstitialLoadFailed;

		public event InterstitialDisplayed InterstitialDisplayed;
		public event InterstitialDisplayFailed InterstitialDisplayFailed;

		public bool CanShowInterstitial(AdPlacementEntry _, out AdShowError? error)
		{
			error = null;

			if (_interstitial.ad == null)
			{
				error = AdShowErrorCode.NotInitialized;
				return false;
			}

			if (_interstitial.status != AdLoadingStatus.Loaded)
			{
				error = AdShowErrorCode.NotLoaded;
				return false;
			}

			if (!_interstitial.ad.IsAdReady())
			{
				error = AdShowErrorCode.NotReady;
				return false;
			}

			return true;
		}

		/// <inheritdoc cref="IAdvertisingIntegration.ShowInterstitial"/>
		public bool ShowInterstitial(in ShowInterstitialArgs args)
		{
			var track = args.track;

			_cacheInterstitialPlacement = args.placement;
			if (!CanShowInterstitial(_cacheInterstitialPlacement, out var errorCode))
			{
				#region Auto Load

				if (errorCode == AdShowErrorCode.NotLoaded && !args.disableAutoLoad)
				{
					if (LoadInterstitial(_cacheInterstitialPlacement))
					{
						_interstitial.ad.OnAdLoadFailed += OnAdLoadFailed;
						_interstitial.ad.OnAdLoaded += OnAdLoaded;

						void OnAdLoaded(LevelPlayAdInfo info)
						{
							_interstitial.ad.OnAdLoaded -= OnAdLoaded;
							_interstitial.ad.OnAdLoadFailed -= OnAdLoadFailed;
							ShowInternal(_cacheInterstitialPlacement);
						}

						void OnAdLoadFailed(LevelPlayAdError error)
						{
							_interstitial.ad.OnAdLoaded -= OnAdLoaded;
							_interstitial.ad.OnAdLoadFailed -= OnAdLoadFailed;
							AdsDebug.LogError(error.ToString());
						}
					}

					return true;
				}

				#endregion

				return false;
			}

			ShowInternal(_cacheInterstitialPlacement);
			return true;

			void ShowInternal(AdPlacementEntry placement)
			{
				var name = placement.GetName(in _platform);
				_interstitial.ad.ShowAd(track ? name : null);
			}
		}

		/// <inheritdoc cref="LoadInterstitialInternal"/>
		public bool LoadInterstitial(AdPlacementEntry _)
		{
			if (_interstitial.status == AdLoadingStatus.Loaded)
				return false;

			return LoadInterstitialInternal();
		}

		/// <returns>Возвращает успешность запроса</returns>
		private bool LoadInterstitialInternal()
		{
			if (_interstitial.status == AdLoadingStatus.Loading)
			{
				AdsDebug.Log($"[{AdPlacementType.Interstitial}] already loading...");
				return false;
			}

			_interstitial.ad.LoadAd();
			SetInterstitialLoadingStatus(AdLoadingStatus.Loading);
			AdsDebug.Log($"[{AdPlacementType.Interstitial}] initiated loading");
			return true;
		}

		public AdLoadingStatus GetInterstitialLoadingStatus(AdPlacementEntry placement) => _interstitial.status;

		private void OnInterstitialClicked(LevelPlayAdInfo info)
			=> InterstitialClicked?.Invoke(_cacheInterstitialPlacement, info);

		private void OnInterstitialClosed(LevelPlayAdInfo info)
		{
			SetInterstitialLoadingStatus(AdLoadingStatus.None);
			InterstitialClosed?.Invoke(_cacheInterstitialPlacement, info);
		}

		private void OnInterstitialLoaded(LevelPlayAdInfo info)
		{
			AdsDebug.Log($"[{AdPlacementType.Interstitial}] loaded");
			SetInterstitialLoadingStatus(AdLoadingStatus.Loaded);
			InterstitialLoaded?.Invoke(info);
		}

		private void OnInterstitialLoadFailed(LevelPlayAdError error)
		{
			SetInterstitialLoadingStatus(AdLoadingStatus.None);
			InterstitialLoadFailed?.Invoke(error.ToString(), error);
		}

		private void OnInterstitialDisplayed(LevelPlayAdInfo info)
			=> InterstitialDisplayed?.Invoke(_cacheInterstitialPlacement, info);

		private void OnInterstitialDisplayFailed(LevelPlayAdDisplayInfoError error)
		{
			SetInterstitialLoadingStatus(AdLoadingStatus.None);
			InterstitialDisplayFailed?.Invoke(_cacheInterstitialPlacement, error.ToString(), error);
		}

		private void SetInterstitialLoadingStatus(AdLoadingStatus status)
		{
			_interstitial.status = status;
		}

		#endregion

		private void OnApplicationPause()
			=> SetPause(true);

		private void OnApplicationResume()
			=> SetPause(false);

		private void OnUserIdChanged(in string userId) => SetUserId(userId);

		private static void SetUserId(string userId)
		{
			IronSource.Agent.setUserId(userId);
			AdsDebug.Log($"Set userId: {userId}");
		}

		private void SetPause(bool value) => IronSource.Agent.onApplicationPause(value);
	}
}
