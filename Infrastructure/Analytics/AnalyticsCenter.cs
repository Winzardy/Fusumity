using System.Runtime.CompilerServices;
using Sapientia;

namespace Analytics
{
	public class AnalyticsCenter : StaticWrapper<AnalyticsManagement>
	{
		// ReSharper disable once InconsistentNaming
		private static AnalyticsManagement management
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		public static bool IsInitialized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance != null;
		}

		public static bool Active => management.Active;

		public static event Receiver<AnalyticsEventPayload> BeforeSend
		{
			add => management.BeforeSend += value;
			remove => management.BeforeSend -= value;
		}

		public static void Send(ref AnalyticsEventPayload payload) => management.Send(ref payload);

		public static bool Register<T>(T aggregator) where T : AnalyticsAggregator
			=> management.Register(aggregator);

		public static bool Unregister<T>(T aggregator) where T : AnalyticsAggregator
			=> management.Unregister(aggregator);
	}
}
