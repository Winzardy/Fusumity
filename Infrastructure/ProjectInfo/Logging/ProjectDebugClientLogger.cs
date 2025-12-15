using Fusumity.Utility;
using ProjectInformation;
using WLog;

namespace Logging.ProjectInformation
{
	public class ProjectClientLogger : BaseLogger
	{
		private const string CHANNEL = "Project";
		private static readonly string PREFIX = string.Format(WLogExtensions.CHANNEL_FORMAT, CHANNEL.ColorTextInEditorOnly(ProjectDebug.COLOR));

		private WLogContext _logContext;
		protected override string Prefix => PREFIX;
		protected override WLogContext LogContext
			=> _logContext ??= WLogContext.Create(CHANNEL, miniStackTracePosition: MiniStackTracePosition.End);

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#else
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
		private static void Setup() => ProjectDebug.logger ??= new ProjectClientLogger();
	}
}
