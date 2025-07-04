using System;
using System.Runtime.CompilerServices;
using Sapientia;

namespace Analytics
{
	public class AnalyticsCenter : StaticProvider<AnalyticsManagement>
	{
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

		public static event Action<AnalyticsEventArgs> BeforeSend
		{
			add => management.BeforeSend += value;
			remove => management.BeforeSend -= value;
		}

		public static void Send(ref AnalyticsEventArgs args) => management.Send(ref args);

		public static bool TryRegister(Type type, out AnalyticsAggregator aggregator)
			=> management.TryRegister(type, out aggregator);
	}
}
