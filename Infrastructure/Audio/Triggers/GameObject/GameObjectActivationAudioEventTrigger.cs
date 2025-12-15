using UnityEngine;

namespace Audio
{
	public class GameObjectActivationAudioEventTrigger : AudioEventTrigger
	{
		[AllowLoop(true)]
		public AudioEventRequest onEnable;

		[Tooltip("Принудительно выключить звук при OnDisable (деактивации) объекта?")]
		[SerializeField]
		private bool _disposeOnDisable = true;

		[Space]
		public AudioEventRequest onDisable;

		private AudioPlayback _playback;

		private void OnEnable()
		{
			if (onEnable.IsEmpty)
				return;

			_playback = onEnable.Play(transform);
		}

		private void OnDisable()
		{
			if (_disposeOnDisable)
				_playback?.Dispose();

			if (onDisable.IsEmpty)
				return;

			onDisable.Play(transform);
		}
	}
}
