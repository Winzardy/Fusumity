using Fusumity.Utility;
using Notifications;
using WLog;

namespace Logging.Notifications
{
	public class NotificationsClientLogger : BaseLogger
	{
		private const string CHANNEL = "Notifications";
		private static readonly string PREFIX = string.Format(WLogExtensions.CHANNEL_FORMAT, CHANNEL.ColorTextInEditorOnly(NotificationsDebug.COLOR));

		private WLogContext _logContext;
		protected override string Prefix => PREFIX;
		protected override WLogContext LogContext
			=> _logContext ??= WLogContext.Create(CHANNEL, miniStackTracePosition: MiniStackTracePosition.End);

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#else
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
		private static void Setup() => NotificationsDebug.logger ??= new NotificationsClientLogger();
	}
}
