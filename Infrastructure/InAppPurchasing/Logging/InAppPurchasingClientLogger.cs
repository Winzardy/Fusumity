using Fusumity.Utility;
using InAppPurchasing;
using WLog;

namespace Logging.InAppPurchasing
{
	public class InAppPurchasingClientLogger : BaseLogger
	{
		private const string CHANNEL = "InAppPurchasing";

		private static readonly string PREFIX = string.Format(WLogExtensions.CHANNEL_FORMAT,
			CHANNEL.ColorTextInEditorOnly(IAPDebug.COLOR));

		private WLogContext _logContext;
		protected override string Prefix => PREFIX;

		protected override WLogContext LogContext
			=> _logContext ??= WLogContext.Create(CHANNEL, miniStackTracePosition: MiniStackTracePosition.End);

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#else
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
		private static void Setup() => IAPDebug.logger ??= new InAppPurchasingClientLogger();
	}
}
