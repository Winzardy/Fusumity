using System;
using Fusumity.Utility;
using Sapientia;
using Sirenix.OdinInspector;

namespace Advertising.Offline
{
	public class OfflineAdvertisingService : IAdvertisingService
	{
		private DateTime Now => DateTime.UtcNow;

		public bool CanShow(AdPlacementKey key, out AdShowError? error)
		{
			error = null;

			if (!key.TryGetEntry(out var entry))
			{
				error = new AdShowError(AdShowErrorCode.NotFoundPlacementEntry);
				return false;
			}

			if (!LocalSave.Has(key))
				return true;

			var model = LocalSave.Load<AdPlacementOfflineModel>(key);

			if (!entry.usageLimit.CanApplyUsage(in model.usageLimit, Now, out var errorCode))
			{
				error = new AdShowError(AdShowErrorCode.UsageLimit, errorCode);
				return false;
			}

			return true;
		}

		public AdvertisingRegisterResult RegisterShow(AdPlacementKey key)
		{
			if (!key.TryGetEntry(out var entry))
				throw new Exception($"Failed to register show: Not found AdPlacementEntry for key [ {key} ]");

			if (entry.usageLimit.IsEmpty())
				return AdvertisingRegisterResult.Done;

			var model = LocalSave.Load<AdPlacementOfflineModel>(key);
			model.usageLimit.ApplyUsage(in entry.usageLimit, Now);
			LocalSave.Save(key, model);
			return AdvertisingRegisterResult.Done;
		}
	}

	[Serializable]
	public struct AdPlacementOfflineModel
	{
		public UsageLimitState usageLimit;
	}
}
