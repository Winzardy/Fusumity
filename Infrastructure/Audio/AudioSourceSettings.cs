using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
	public struct AudioSourceSettings
	{
		public AudioMixerGroup mixerGroup;
		public bool loop;
		public bool bypassEffects;
		public bool bypassListenerEffects;
		public bool bypassReverbZones;
		public int priority;
		public float stereoPan;
		public float reverbZoneMix;
		public float minDistance;
		public float maxDistance;
		public float spatialBlend;
		public float dopplerLevel;
		public int spread;
		public AudioRolloffMode rolloffMode;
		public AnimationCurve customRolloffCurve;
		public bool timeScaledPitch;

		public AudioSourceSettings(AudioMixerGroup mixerGroup = null,
			bool bypassEffects = false,
			bool bypassListenerEffects = false,
			bool bypassReverbZones = false,
			bool loop = false,
			float maxDistance = 500,
			float minDistance = 0,
			float stereoPan = 0,
			int priority = 0,
			float reverbZoneMix = 1,
			float spatialBlend = 1,
			float dopplerLevel = 1,
			int spread = 50,
			AudioRolloffMode rolloffMode = AudioRolloffMode.Linear,
			AnimationCurve customRolloffCurve = null,
			bool timeScaledPitch = false)
		{
			this.mixerGroup = mixerGroup;
			this.bypassEffects = bypassEffects;
			this.bypassListenerEffects = bypassListenerEffects;
			this.bypassReverbZones = bypassReverbZones;
			this.loop = loop;
			this.priority = priority;
			this.stereoPan = stereoPan;
			this.spatialBlend = spatialBlend;
			this.dopplerLevel = dopplerLevel;
			this.spread = spread;
			this.reverbZoneMix = reverbZoneMix;
			this.minDistance = minDistance;
			this.maxDistance = maxDistance;
			this.rolloffMode = rolloffMode;
			this.customRolloffCurve = customRolloffCurve;
			this.timeScaledPitch = timeScaledPitch;
		}

		public readonly void Apply(AudioSource source)
		{
			source.outputAudioMixerGroup = mixerGroup;
			source.bypassEffects = bypassEffects;
			source.bypassListenerEffects = bypassListenerEffects;
			source.bypassReverbZones = bypassReverbZones;
			source.dopplerLevel = 0;
			source.loop = loop;
			source.priority = priority;
			source.panStereo = stereoPan;
			source.reverbZoneMix = reverbZoneMix;

			source.spatialBlend = spatialBlend;

			source.dopplerLevel = dopplerLevel;
			source.spread = spread;
			source.minDistance = minDistance;
			source.maxDistance = maxDistance;
			source.rolloffMode = rolloffMode;

			if (rolloffMode == AudioRolloffMode.Custom)
				source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, customRolloffCurve);
		}
	}
}
