using System;
using Fusumity.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Advertising.UnityLevelPlay
{
	[Serializable]
	public struct UnityLevelPlaySettings
	{
		[DictionaryDrawerSettings(KeyLabel = "Platform", ValueLabel = "Entry")]
		public SerializableDictionary<UnityLevelPlaySupportedPlatform, UnityLevelPlayPlatformEntry> platformToEntry;

		[Space]
		[Tooltip("Контролировать загрузку видео рекламы вручную. В IronSource вшита автоматическая загрузка следующей рекламы после ее просмотра")]
		public bool manualLoadRewardedVideo;

		[Tooltip("Отключить отслеживания изменения сети и управление запросами на загрузку рекламы из-за сети")]
		public bool disableTrackNetworkState;
	}

	[Serializable]
	public struct UnityLevelPlayPlatformEntry
	{
		public string appKey;

		[Space]
		public string rewardAdUnitId;

		public string interstitialAdUnitId;
	}
}
