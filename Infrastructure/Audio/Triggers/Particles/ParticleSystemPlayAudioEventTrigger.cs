using Fusumity.Attributes.Specific;
using Fusumity.Reactive;
using UnityEngine;

namespace Audio
{
	public sealed class ParticleSystemPlayAudioEventTrigger : AudioEventTrigger
	{
		[SerializeField]
		private ParticleSystem _particleSystem;

		[AllowLoop(true)]
		public AudioEventRequest onPlay;

		public AudioPlaybackPositionMode playMode = AudioPlaybackPositionMode.FollowTarget;

		[HideIf(nameof(IsLoop))]
		public bool disposeOnDestroy;

		private bool IsLoop => onPlay.loop;

		private AudioPlayback _playback;
		private bool _wasPlaying;

		private void Awake()
		{
			UnityLifecycle.FixedUpdateEvent.Subscribe(HandleFixedUpdateEvent);
		}

		private void OnDestroy()
		{
			UnityLifecycle.FixedUpdateEvent.UnSubscribe(HandleFixedUpdateEvent);
		}

		private void HandleFixedUpdateEvent()
		{
			if (_particleSystem == null || onPlay.IsEmpty)
				return;

			bool isPlaying = _particleSystem.isPlaying;

			if (!_wasPlaying && isPlaying)
			{
				_playback = onPlay.Play(transform, playMode);
			}

			_wasPlaying = isPlaying;
		}

		private void OnEnable()
		{
			_wasPlaying = false;
		}

		private void OnDisable()
		{
			if (disposeOnDestroy || onPlay.loop)
				_playback?.Dispose();
			_playback = null;
		}
	}
}
