using Fusumity.Utility;
using SharedLogic;
using UnityEditor;
using WLog;

namespace Logging.SharedLogic
{
	public class SharedLogicClientLogger : BaseLogger
	{
		private const string CHANNEL = "SharedLogic";

		private static readonly string PREFIX = string.Format(WLogExtensions.CHANNEL_FORMAT, CHANNEL
		   .ColorTextInEditorOnly(SLDebug.COLOR));

		private WLogContext _logContext;
		protected override string Prefix => PREFIX;

		protected override WLogContext LogContext
			=> _logContext ??= WLogContext.Create(CHANNEL, miniStackTracePosition: MiniStackTracePosition.End);

#if UNITY_EDITOR
		[InitializeOnLoadMethod]
#else
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
		private static void Setup() => SLDebug.logger ??= new SharedLogicClientLogger();
	}
}
