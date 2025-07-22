using System;
using Audio;
using Fusumity.Reactive;

namespace Booting.Audio
{
	public class AudioEngineEvents : IAudioEngineEvents
	{
		public void Subscribe(EventsType type, Action action)
		{
			switch (type)
			{
				case EventsType.Update:
					UnityLifecycle.UpdateEvent.Subscribe(action);
					break;
				case EventsType.LateUpdate:
					UnityLifecycle.LateUpdateEvent.Subscribe(action);
					break;
			}
		}

		public void Unsubscribe(EventsType type, Action action)
		{
			switch (type)
			{
				case EventsType.Update:
					UnityLifecycle.UpdateEvent.UnSubscribe(action);
					break;
				case EventsType.LateUpdate:
					UnityLifecycle.LateUpdateEvent.UnSubscribe(action);
					break;
			}
		}
	}
}
