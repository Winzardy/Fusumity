using System;
using Sapientia.Extensions;
using Targeting;
using UnityEngine;

namespace InAppReview
{
	public interface IInAppReviewStorePlatform
	{
		public void RequestReview(bool useStorePage = true);
	}

	public abstract class BaseInAppReviewStorePlatform : IInAppReviewStorePlatform, IDisposable
	{
		private bool _useStorePage;

		public void RequestReview(bool useStorePage = true)
		{
			_useStorePage = useStorePage;
			OnRequestReview();
		}

		protected virtual void OnRequestReview()
		{
		}

		public void Dispose() => OnDispose();

		protected virtual void OnDispose()
		{
		}

		protected void TryOpenStorePage()
		{
			if (!_useStorePage)
				return;

			var link = Desk.GetReviewLink();

			if (!link.IsNullOrEmpty())
				Application.OpenURL(link);
		}
	}
}
