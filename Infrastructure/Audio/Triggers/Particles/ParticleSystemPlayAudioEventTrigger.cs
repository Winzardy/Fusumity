using UnityEngine;

namespace Audio
{
	public sealed class ParticleSystemPlayAudioEventTrigger : AudioEventTrigger
	{
		[SerializeField] private ParticleSystem _particleSystem;

		[AllowLoop(true)] public AudioEventRequest onPlay;

		private AudioPlayback _playback;
		private bool _wasPlaying;

		private void OnEnable()
		{
			_wasPlaying = false;
		}

		private void Update()
		{
			if (_particleSystem == null || onPlay.IsEmpty)
				return;

			bool isPlaying = _particleSystem.isPlaying;

			if (!_wasPlaying && isPlaying)
			{
				_playback = onPlay.Play(transform);
			}

			_wasPlaying = isPlaying;
		}

		private void OnDisable()
		{
			_playback?.Dispose();
		}
	}
}
