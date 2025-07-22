using System;
using Audio;
using Fusumity.Utility;
using UnityEngine;

namespace Booting.Audio
{
	public class DefaultAudioListenerOwner : IDisposable, IAudioListenerOwner
	{
		private const string LISTENER_NAME = "AudioListener";

		private readonly AudioFactory _factory;
		private AudioListener _listener;

		int IAudioListenerOwner.Priority => 0;
		AudioListener IAudioListenerOwner.Listener => _listener;

		public DefaultAudioListenerOwner(AudioFactory factory)
		{
			_factory = factory;
			_listener = _factory.CreateListener(LISTENER_NAME);
		}

		public void Dispose() => _listener.DestroySafe();

		public override string ToString() => $"{_listener.name} - {0}";
	}
}
