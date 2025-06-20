using Sapientia.Collections;
using Sapientia.Extensions;
using UnityEngine;

namespace Fusumity.Reactive
{
	public partial class UnityLifecycle
	{
		private static SimpleList<TimeEvent> _timeEvents = new();
		private static SimpleList<TimeEvent> _unscaledTimeEvents = new();

		public static void ScheduleTimeEvent(TimeEvent timeEvent)
		{
			if (timeEvent.IsDefault())
				return;
#if UNITY_EDITOR
			var oldCapacity = _timeEvents.Capacity;
#endif
			if (timeEvent.isUnscaled)
				_unscaledTimeEvents.BinaryInsert(timeEvent);
			else
				_timeEvents.BinaryInsert(timeEvent);
#if UNITY_EDITOR
			if (oldCapacity < _timeEvents.Capacity)
				Debug.LogWarning($"UnityEventHandler TimeEvent list expanded: {oldCapacity}->{_timeEvents.Capacity}");
#endif
		}

		public static void CancelTimeEvent(TimeEvent timeEvent)
		{
			if (timeEvent.IsDefault())
				return;

			if (timeEvent.isUnscaled)
				_unscaledTimeEvents.BinaryRemove(timeEvent);
			else
				_timeEvents.BinaryRemove(timeEvent);
		}
	}
}
