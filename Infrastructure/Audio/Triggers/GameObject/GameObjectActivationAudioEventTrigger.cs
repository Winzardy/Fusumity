using Fusumity.Reactive;
using Fusumity.Utility;
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

		private bool _isFrameLocked;

		private void OnEnable()
		{
			if (_isFrameLocked)
				return;

			_isFrameLocked = true;

			if (onEnable.IsEmpty)
				return;

			_playback = onEnable.Play(transform);

			UnityLifecycle.LateExecuteOnceEvent += HandleLateExecuteOnceEvent;
		}

		private void OnDisable()
		{
			if (_isFrameLocked)
				return;

			OnDisabled();
		}

		private void HandleLateExecuteOnceEvent()
		{
			_isFrameLocked = false;

			if (!gameObject.IsActive())
				OnDisabled();
		}

		private void OnDisabled()
		{
			if (_disposeOnDisable)
				_playback?.Dispose();

			if (onDisable.IsEmpty)
				return;

			onDisable.Play(transform);
		}
	}
}
