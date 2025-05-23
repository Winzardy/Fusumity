using System.Collections;
using Fusumity.Reactive;
using Google.Play.Review;

namespace InAppReview.GooglePlay
{
	public class GooglePlayInAppReview : BaseInAppReviewStorePlatform
	{
		private readonly ReviewManager _reviewManager = new();
		private IEnumerator _requestRoutine;

		protected override void OnDispose()
		{
			if (_requestRoutine != null)
				UnityLifecycle.CancelCoroutine(_requestRoutine);
		}

		protected override void OnRequestReview()
		{
			if (_requestRoutine != null)
				return;

			_requestRoutine = RequestRoutine();
			UnityLifecycle.ExecuteCoroutine(_requestRoutine);
		}

		private IEnumerator RequestRoutine()
		{
			var requestFlowOperation = _reviewManager.RequestReviewFlow();
			yield return requestFlowOperation;

			if (requestFlowOperation.Error != ReviewErrorCode.NoError)
			{
				InAppReviewDebug.LogWarning($"Native request errorCode: {requestFlowOperation.Error}");

				_requestRoutine = null;
				TryOpenStorePage();
				yield break;
			}

			var info = requestFlowOperation.GetResult();
			var flowOperation = _reviewManager.LaunchReviewFlow(info);

			yield return flowOperation;

			if (flowOperation.Error != ReviewErrorCode.NoError)
			{
				InAppReviewDebug.LogWarning($"Native launch errorCode: {flowOperation.Error}");

				_requestRoutine = null;
				TryOpenStorePage();
				yield break;
			}

			InAppReviewDebug.Log("Native launch result (without error): " +
				$"IsSuccessful - {flowOperation.IsSuccessful}, " +
				$"IsDone - {flowOperation.IsDone}");

			_requestRoutine = null;
		}
	}
}
