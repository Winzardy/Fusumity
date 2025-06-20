using UnityEngine.iOS;

namespace InAppReview.AppStore
{
	public class AppStoreInAppReview : BaseInAppReviewStorePlatform
	{
		protected override void OnRequestReview()
		{
			var result = Device.RequestStoreReview();

			if (!result)
				TryOpenStorePage();

			InAppReviewDebug.Log($"Native launch result: {result}");
		}
	}
}
