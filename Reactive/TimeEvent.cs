using System;
using UnityEngine;

namespace Fusumity.Reactive
{
	[Obsolete("@vevdokimov не советую использовать, очень странно, есть async...")]
	public struct TimeEvent : IComparable<TimeEvent>, IEquatable<TimeEvent>
	{
		public float beginTime;
		public float endTime;
		public bool isUnscaled;
		public Action action;

		private TimeEvent(float beginTime, Action action, bool isUnscaled = false)
		{
			this.beginTime = beginTime;
			this.endTime = beginTime;
			this.action = action;
			this.isUnscaled = isUnscaled;
		}

		private TimeEvent(float beginTime, float endTime, Action action, bool isUnscaled = false)
		{
			this.beginTime = beginTime;
			this.endTime = endTime;
			this.action = action;
			this.isUnscaled = isUnscaled;
		}

		public static TimeEvent CreateDelayEvent(Action action, float delay, bool isUnscaled = false)
		{
			var time = (isUnscaled ? Time.unscaledTime : Time.time) + delay;
			return new TimeEvent()
			{
				action = action,
				beginTime = time,
				endTime = time,
				isUnscaled = isUnscaled,
			};
		}

		public void Schedule()
		{
			UnityLifecycle.ScheduleTimeEvent(this);
		}

		public void Cancel()
		{
			UnityLifecycle.CancelTimeEvent(this);
		}

		public int CompareTo(TimeEvent other)
		{
			var timeCompareResult = beginTime.CompareTo(other.beginTime);
			if (timeCompareResult == 0)
				timeCompareResult = endTime.CompareTo(other.endTime);
			return timeCompareResult;
		}

		public bool Equals(TimeEvent other)
		{
			return beginTime.Equals(other.beginTime) && endTime.Equals(other.endTime) && Equals(action, other.action);
		}
	}
}
