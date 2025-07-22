using Advertising;

namespace Logging.Advertising
{
	public class AdvertisingClientLogger : BaseLogger<AdsDebug>
	{
		private static readonly string PREFIX = DebugJournal.GetChannelPrefix("Advertising", AdsDebug.COLOR);

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#else
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
		private static void Setup() => AdsDebug.logger ??= new AdvertisingClientLogger();

		protected override string prefix => PREFIX;
	}
}