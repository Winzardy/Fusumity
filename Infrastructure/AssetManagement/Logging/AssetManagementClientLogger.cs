using AssetManagement;
using Fusumity.Utility;
using WLog;

namespace Logging.AssetManagement
{
	public class AssetManagementClientLogger : BaseLogger
	{
		private const string CHANNEL = "Assets";
		private static readonly string PREFIX = string.Format(WLogExtensions.CHANNEL_FORMAT, CHANNEL.ColorTextInEditorOnly(AssetManagementDebug.COLOR));

		private WLogContext _logContext;
		protected override string Prefix => PREFIX;
		protected override WLogContext LogContext
			=> _logContext ??= WLogContext.Create(CHANNEL, miniStackTracePosition: MiniStackTracePosition.End);

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#else
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
		private static void Setup() => AssetManagementDebug.logger ??= new AssetManagementClientLogger();
	}
}
