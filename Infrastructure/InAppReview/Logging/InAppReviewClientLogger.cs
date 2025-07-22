using Fusumity.Utility;
using InAppReview;
using WLog;

namespace Logging.InAppReview
{
	public class InAppReviewClientLogger : BaseLogger
	{
		private const string CHANNEL = "InAppReview";
		private static readonly string PREFIX = string.Format(WLogExtensions.CHANNEL_FORMAT, CHANNEL.ColorTextInEditorOnly(InAppReviewDebug.COLOR));

		private WLogContext _logContext;
		protected override string Prefix => PREFIX;
		protected override WLogContext LogContext
			=> _logContext ??= WLogContext.Create(CHANNEL, miniStackTracePosition: MiniStackTracePosition.End);

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#else
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
		private static void Setup() => InAppReviewDebug.logger ??= new InAppReviewClientLogger();
	}
}
