using System;
using System.Collections.Generic;
using Audio;
using Fusumity.Utility;
using UnityEngine;

namespace Booting.Audio
{
	public class AudioListenerLocator : IAudioListenerLocator
	{
		private const string LISTENER_NAME = "AudioListener";

		private IAudioListenerOwner _current;
		private readonly List<IAudioListenerOwner> _listeners = new();

		public event Action<AudioListener> Updated;

		public AudioListener Get() => _current?.Listener;

		public void Register(IAudioListenerOwner owner)
		{
			_listeners.Add(owner);

			if (_current != null && owner.Priority < _current.Priority)
				return;

			SetCurrent(owner);
		}

		public void Unregister(IAudioListenerOwner owner)
		{
			if (!_listeners.Remove(owner))
				return;

			if (_current != owner)
				return;

			_current = null;
			SetCurrentByPriority();
		}

		private void SetCurrentByPriority()
		{
			IAudioListenerOwner newCurrent = null;
			foreach (var target in _listeners)
			{
				if (newCurrent != null && newCurrent.Priority == target.Priority)
					AudioDebug.LogWarning(
						$"With the same priority, the current audio listener is not deterministic! ({target},{newCurrent})",
						target.Listener);
				if (newCurrent == null || target.Priority > newCurrent.Priority)
					newCurrent = target;
			}

			if (newCurrent != null)
				SetCurrent(newCurrent);
		}

		private void SetCurrent(IAudioListenerOwner owner)
		{
			_current?.Listener.SetEnableSafe(false);
			var prev = _current;

			_current = owner;
			_current.Listener.SetEnableSafe(true);

			if (prev != owner)
				Updated?.Invoke(Get());
		}
	}
}
