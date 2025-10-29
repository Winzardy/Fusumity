using System;
using System.Collections.Generic;
using AssetManagement;
using Sirenix.OdinInspector;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace Audio
{
	[Serializable]
	public class AudioSettings
	{
		public MasterMixerEntry masterMixer;

		/// <summary>
		/// Плееры (AudioPlayer) которые не нужно включать (GetType().ToString())
		/// </summary>
		public List<string> disableAudioPlayers;

		public string MasterMixerName => masterMixer.reference.Path;
	}

	[Serializable]
	public struct MasterMixerEntry
	{
		/// <summary>
		/// Используется ресурсы юнити, потому что Mixer через подгрузку Addressables не работает
		/// </summary>
		public ResourceReferenceEntry<AudioMixer> reference;

		[FormerlySerializedAs("entry")]
		[HideLabel]
		public AudioMixerGroupConfig config;
	}
}
