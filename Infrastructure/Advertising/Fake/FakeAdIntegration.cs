using System;
using AssetManagement;
using Cysharp.Threading.Tasks;
using Fusumity.Utility;
using Sapientia.Extensions;
using Object = UnityEngine.Object;

namespace Advertising.Fake
{
	public class FakeAdIntegration : IAdvertisingIntegration, IDisposable
	{
		private const int OVERLAY_FRAMES = 100;
		public const bool DEFAULT_USE_OVERLAY = true;

		private bool _disposed;
		private bool _closeRequested;

		private FakeAdSettings _settings;
		private bool _useOverlay = DEFAULT_USE_OVERLAY;
		private FakeOverlay _overlay;

		public string Name => "Fake";

		public bool UseOverlay => _useOverlay;

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

		public FakeAdIntegration(FakeAdSettings? settings = null)
		{
			_settings = settings ?? FakeAdSettings.Default;
			LoadOverlayAsync().Forget();
		}

		public void Dispose()
		{
			_disposed = true;

			if (!_overlay)
				return;

			_overlay.CloseClicked += OnOverlayCloseClicked;
		}

		public bool CanShowRewarded(AdPlacementEntry placement, out AdShowError? error)
		{
			error = null;
			return true;
		}

		public bool ShowRewarded(in ShowRewardedArgs args)
		{
			RewardedDisplayed?.Invoke(args.placement);
			ShowRewardedAsync(args).Forget();
			return true;
		}

		private async UniTaskVoid ShowRewardedAsync(ShowRewardedArgs args)
		{
			var placement = args.placement;

			if (_overlay)
			{
				var full = await ShowOverlayAsync(placement, _settings.rewardedDelayMs);
				if (!full)
				{
					RewardedClosed?.Invoke(args.placement, false);
					return;
				}
			}
			else
				await UniTask.Delay(_settings.rewardedDelayMs, DelayType.Realtime);

			RewardedCompleted?.Invoke(args.placement);
			RewardedClosed?.Invoke(args.placement, true);
		}

		public bool LoadRewarded(AdPlacementEntry placement) => true;
		public AdLoadingStatus GetRewardedLoadingStatus(AdPlacementEntry placement) => AdLoadingStatus.Loaded;

		public bool CanShowInterstitial(AdPlacementEntry placement, out AdShowError? error)
		{
			error = null;
			return true;
		}

		public bool ShowInterstitial(in ShowInterstitialArgs args)
		{
			InterstitialDisplayed?.Invoke(args.placement);
			ShowInterstitialAsync(args).Forget();
			return true;
		}

		private async UniTaskVoid ShowInterstitialAsync(ShowInterstitialArgs args)
		{
			var placement = args.placement;

			if (_overlay)
				await ShowOverlayAsync(placement, _settings.interstitalDelayMs);
			else
				await UniTask.Delay(_settings.interstitalDelayMs, DelayType.Realtime);

			InterstitialClosed?.Invoke(placement);
		}

		/// <returns>Полностью или закрыли</returns>
		private async UniTask<bool> ShowOverlayAsync(AdPlacementEntry placement, int delayMs)
		{
			if (!_useOverlay)
				return true;

			_overlay.SetActive(true);
			for (var i = 0; i < OVERLAY_FRAMES; i++)
			{
				var frameDelayMs = delayMs / OVERLAY_FRAMES;
				await UniTask.Delay(frameDelayMs, DelayType.Realtime);

				if (_closeRequested)
				{
					_overlay.SetActive(false);
					_closeRequested = false;
					return false;
				}

				var leftMs = delayMs - frameDelayMs * i;
				var leftStr = leftMs.ToSec().ToString("F2").PercentSizeText(130);
				_overlay.SetText($"[{placement.Type}] {placement.Id} \n" +
					$"{leftStr}\n" +
					"left seconds".PercentSizeText(80));
			}

			_overlay.SetActive(false);
			return true;
		}

		public void SetOverlay(bool active)
		{
			if (_useOverlay == active)
				return;

			_useOverlay = active;
			AdsDebug.Log($"Fake overlay: {active}");
		}

		public bool LoadInterstitial(AdPlacementEntry placement) => true;
		public AdLoadingStatus GetInterstitialLoadingStatus(AdPlacementEntry placement) => AdLoadingStatus.Loaded;

		private async UniTaskVoid LoadOverlayAsync()
		{
			var prefab = await _settings.overlayReference.LoadAsync();

			if (_disposed)
			{
				_settings.overlayReference.Release();
				return;
			}

			_overlay = Object.Instantiate(prefab);
			if (_overlay)
			{
				_overlay.DontDestroyOnLoad();
				_overlay.SetActive(false);
			}

			_settings.overlayReference.Release();

			_overlay.CloseClicked += OnOverlayCloseClicked;
		}

		private void OnOverlayCloseClicked() => _closeRequested = true;
	}

	[Serializable]
	public struct FakeAdSettings
	{
		public int rewardedDelayMs;
		public int interstitalDelayMs;

		public ResourceReferenceEntry<FakeOverlay> overlayReference;

		public static FakeAdSettings Default => new()
		{
			rewardedDelayMs = 5000,
			interstitalDelayMs = 3000,
			overlayReference = "FakeAdOverlay"
		};
	}
}
