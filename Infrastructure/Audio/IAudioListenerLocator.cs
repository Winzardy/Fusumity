using System;
using UnityEngine;

namespace Audio
{
	public interface IAudioListenerLocator
	{
		public event Action<AudioListener> Updated;
		public AudioListener Get();

		public void Register(IAudioListenerOwner owner);
		public void Unregister(IAudioListenerOwner owner);
	}

	public interface IAudioListenerOwner
	{
		public int Priority { get; }
		public AudioListener Listener { get; }
	}
}
