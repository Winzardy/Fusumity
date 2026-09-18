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

		/// <remarks>
		/// Подписка и отписка переживают отсутствие центра: агрегаторы умирают вместе со своим контекстом,
		/// а он может закрыться уже после того, как бут-таск снял аналитику
		/// </remarks>
		public static event Receiver<AnalyticsEventPayload> BeforeSend
		{
			add
			{
				if (!IsInitialized)
				{
					AnalyticsDebug.LogWarning($"Subscribe to [ {nameof(BeforeSend)} ] skipped, analytics center is missing");
					return;
				}

				management.BeforeSend += value;
			}
			remove
			{
				if (!IsInitialized)
				{
					AnalyticsDebug.LogWarning($"Unsubscribe from [ {nameof(BeforeSend)} ] skipped, analytics center is missing");
					return;
				}

				management.BeforeSend -= value;
			}
		}

		public static void Send(ref AnalyticsEventPayload payload) => management.Send(ref payload);

		public static bool Register<T>(T aggregator) where T : AnalyticsAggregator
		{
			if (!IsInitialized)
			{
				AnalyticsDebug.LogWarning($"Register skipped for aggregator [ {aggregator.GetType().Name} ], analytics center is missing");
				return false;
			}

			return management.Register(aggregator);
		}

		public static bool Unregister<T>(T aggregator) where T : AnalyticsAggregator
		{
			if (!IsInitialized)
			{
				AnalyticsDebug.LogWarning($"Unregister skipped for aggregator [ {aggregator.GetType().Name} ], analytics center is missing");
				return false;
			}

			return management.Unregister(aggregator);
		}
	}
}
