using System;
using Fusumity.Utility;
using Sapientia;

namespace SharedLogic
{
	public class SharedDateTimeProvider : IDateTimeProvider
	{
		private const string LOCAL_SAVE_DELTA_CACHE_KEY = "server_datetime_delta_cache";

		private TimeSpan _timeDelta;

		public DateTime DateTime => DateTime.UtcNow + _timeDelta;

		public SharedDateTimeProvider()
		{
			// Установить дельту из локального кэша
			SetDelta(LocalSave.Load(LOCAL_SAVE_DELTA_CACHE_KEY, DateTime.UtcNow - DateTime.UtcNow));
		}

		public void Setup(DateTime newServerTime)
		{
			SetDelta(newServerTime - DateTime.UtcNow);
		}

		private void SetDelta(TimeSpan delta)
		{
			_timeDelta = delta;
			LocalSave.Save(LOCAL_SAVE_DELTA_CACHE_KEY, _timeDelta);
		}
	}
}
