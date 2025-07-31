using InAppPurchasing;

namespace Logging.InAppPurchasing
{
	public class InAppPurchasingClientLogger : BaseLogger<IAPDebug>
	{
		private static readonly string PREFIX = DebugJournal.GetChannelPrefix("InAppPurchasing", IAPDebug.COLOR);

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#else
		[UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
		private static void Setup() => IAPDebug.logger ??= new InAppPurchasingClientLogger();

		protected override string prefix => PREFIX;
	}
}