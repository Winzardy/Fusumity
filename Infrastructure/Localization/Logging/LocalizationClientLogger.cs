using Fusumity.Utility;
using Localization;
using WLog;

namespace Logging.Localization
{
	public class LocalizationClientLogger : BaseLogger
	{
		private const string CHANNEL = "Localization";
		private static readonly string PREFIX = string.Format(WLogExtensions.CHANNEL_FORMAT, CHANNEL.ColorTextInEditorOnly(LocalizationDebug.COLOR));

		private WLogContext _logContext;
		protected override string Prefix => PREFIX;
		protected override WLogContext LogContext
			=> _logContext ??= WLogContext.Create(CHANNEL, miniStackTracePosition: MiniStackTracePosition.End);

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#else
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
		private static void Setup() => LocalizationDebug.logger ??= new LocalizationClientLogger();
	}
}
