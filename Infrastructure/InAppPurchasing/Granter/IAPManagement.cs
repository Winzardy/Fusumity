#if DebugLog
#define IAP_DEBUG
#endif

using System.Runtime.CompilerServices;

namespace InAppPurchasing
{
	public partial class IAPManager
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool RegisterGranter<T>(T granter) where T : IIAPPurchaseGranter => management.RegisterGranter(granter);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool UnregisterGranter<T>(T granter) where T : IIAPPurchaseGranter => management.UnregisterGranter(granter);

#if IAP_DEBUG
		public static IInAppPurchasingGrantCenter GrantCenter => management.GrantCenter;
#endif
	}

	public partial class IAPManagement
	{
		private IInAppPurchasingGrantCenter _grantCenter;
		internal IInAppPurchasingGrantCenter GrantCenter => _grantCenter;

		public void SetGrantCenter(IInAppPurchasingGrantCenter grantCenter)
		{
			_grantCenter = grantCenter;
		}

		internal bool RegisterGranter<T>(T granter) where T : IIAPPurchaseGranter => _grantCenter.Register(granter);
		internal bool UnregisterGranter<T>(T granter) where T : IIAPPurchaseGranter => _grantCenter.Unregister(granter);
	}
}
