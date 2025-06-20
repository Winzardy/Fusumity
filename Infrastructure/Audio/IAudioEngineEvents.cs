using System;

namespace Audio
{
	public enum EventsType
	{
		Update,
		LateUpdate
	}

	public interface IAudioEngineEvents
	{
		public void Subscribe(EventsType type, Action action);
		public void Unsubscribe(EventsType type, Action action);
	}
}
