using UnityEngine;

namespace Audio
{
	public class AudioListenerOwner : MonoBehaviour, IAudioListenerOwner
	{
		[SerializeField]
		private AudioListener _listener;

		[SerializeField]
		private int _priority = 1;

		private void OnEnable() => AudioManager.Register(this);
		private void OnDisable() => AudioManager.Unregister(this);

		int IAudioListenerOwner.Priority => _priority;
		AudioListener IAudioListenerOwner.Listener => _listener;

		public override string ToString() => $"{gameObject.name} - {_priority}";
	}
}
