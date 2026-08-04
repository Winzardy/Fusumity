using System;
using System.Diagnostics;
using Fusumity.Utility;
using Sapientia;
using UnityEngine;

namespace SharedLogic
{
	public class SharedDateTimeProvider : ISystemTimeProvider
	{
		private const string LOCAL_SAVE_DELTA_CACHE_KEY = "server_datetime_delta_cache";

		private TimeSpan _delta;

		private DateTime _anchorDateTime;
		private long _anchorTimestamp;

		private int _cachedFrame = int.MinValue;
		private DateTime _cachedSystemTime;

		public DateTime SystemTime
		{
			get
			{
				var frame = Time.frameCount;
				if (_cachedFrame == frame)
					return _cachedSystemTime;

				_cachedFrame = frame;
				_cachedSystemTime = GetNowTime() + _delta;
				return _cachedSystemTime;
			}
		}

		public SharedDateTimeProvider()
		{
			var cachedDelta = LocalSave.Load(LOCAL_SAVE_DELTA_CACHE_KEY, TimeSpan.Zero);
			ApplyDelta(cachedDelta);
		}

		public void Synchronize(DateTime dateTime)
		{
			dateTime = NormalizeDateTime(dateTime);
			ApplyDelta(dateTime - DateTime.UtcNow);
		}

		private void ApplyDelta(TimeSpan delta)
		{
			_delta = delta;

			_anchorDateTime = DateTime.UtcNow;
			_anchorTimestamp = Stopwatch.GetTimestamp();
			_cachedFrame = int.MinValue;

			LocalSave.Save(LOCAL_SAVE_DELTA_CACHE_KEY, delta);
		}

		private DateTime GetNowTime() => _anchorDateTime + GetElapsedTime(_anchorTimestamp);

		private static TimeSpan GetElapsedTime(long timestamp)
		{
			var elapsedTicks = Stopwatch.GetTimestamp() - timestamp;
			var elapsedSeconds = elapsedTicks / (double) Stopwatch.Frequency;
			return TimeSpan.FromSeconds(elapsedSeconds);
		}

		private static DateTime NormalizeDateTime(DateTime dateTime)
		{
			return dateTime.Kind switch
			{
				DateTimeKind.Local => dateTime.ToUniversalTime(),
				DateTimeKind.Utc => dateTime,
				_ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
			};
		}
	}
}
