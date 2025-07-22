using Analytics;
using Fusumity.Utility;
using WLog;

namespace Logging.Analytics
{
	public class AnalyticsClientLogger : BaseLogger
	{
		private const string CHANNEL = "Analytics";
		private static readonly string PREFIX = string.Format(WLogExtensions.CHANNEL_FORMAT, CHANNEL.ColorTextInEditorOnly(AnalyticsDebug.COLOR));

		private WLogContext _logContext;
		protected override string Prefix => PREFIX;
		protected override WLogContext LogContext
			=> _logContext ??= WLogContext.Create(CHANNEL, miniStackTracePosition: MiniStackTracePosition.End);

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#else
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
		private static void Setup() => AnalyticsDebug.logger ??= new AnalyticsClientLogger();
	}
}
