using System.Runtime.CompilerServices;
using Sapientia;

namespace InAppReview
{
	public class InAppReviewManager : StaticWrapper<IInAppReviewStorePlatform>
	{
		// ReSharper disable once InconsistentNaming
		private static IInAppReviewStorePlatform storePlatform
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		public static bool IsSupport
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => storePlatform != null;
		}

		public static void RequestReview(bool useStorePage = true) => storePlatform.RequestReview(useStorePage);
	}

	public struct InAppReviewRequestMessage
	{
	}
}
