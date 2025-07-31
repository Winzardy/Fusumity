using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Sirenix.OdinInspector;
using InAppReview;
#if USE_GOOGLE_PLAY
using InAppReview.GooglePlay;
#elif UNITY_IOS
using InAppReview.AppStore;
#endif

namespace Booting.InAppReview
{
	[TypeRegistryItem(
		"\u2009In App Review", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.StarHalf)]
	[Serializable]
	public class InAppReviewBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 130;

		public override UniTask RunAsync(CancellationToken token = default)
		{
			IInAppReviewStorePlatform platform = null;

#if USE_GOOGLE_PLAY
			platform = new GooglePlayInAppReview();
#elif UNITY_IOS
			platform = new AppStoreInAppReview();
#endif

			InAppReviewManager.Initialize(platform);

			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			if (UnityLifecycle.ApplicationQuitting)
				return;

			InAppReviewManager.Terminate();
		}
	}
}
