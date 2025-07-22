using Fusumity.Utility;
using UI;
using WLog;

namespace Logging.UI
{
	public class GUIClientLogger : BaseLogger
	{
		private const string CHANNEL = "UI";
		private static readonly string PREFIX = string.Format(WLogExtensions.CHANNEL_FORMAT, CHANNEL.ColorTextInEditorOnly(GUIDebug.COLOR));

		private WLogContext _logContext;
		protected override string Prefix => PREFIX;
		protected override WLogContext LogContext
			=> _logContext ??= WLogContext.Create(CHANNEL, miniStackTracePosition: MiniStackTracePosition.End);

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#else
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
		private static void Setup()
			=> GUIDebug.logger ??= new GUIClientLogger();
	}
}
