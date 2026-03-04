using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Sapientia;

namespace Advertising
{
	public class AdManager : StaticWrapper<AdManagement>
	{
		private static AdManagement management
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		public static bool IsInitialized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance != null;
		}

		public static IAdEvents Events => management.Events;

		public static bool CanShow<T>(string placement, out AdShowError? error)
			where T : AdPlacementEntry
			=> management.CanShow<T>(placement, out error);

		public static bool CanShow(AdPlacementEntry entry, out AdShowError? error)
			=> management.CanShow(entry, out error);

		public static bool CanShow(AdPlacementType type, string placement, out AdShowError? error)
			=> management.CanShow(type, placement, out error);

		#region Show

		/// <inheritdoc cref="Show(Advertising.AdPlacementEntry,bool)"/>
		public static bool Show<T>(string placement, bool autoLoad = true) where T : AdPlacementEntry
			=> management.Show<T>(placement, autoLoad);

		/// <inheritdoc cref="Show(Advertising.AdPlacementEntry,bool)"/>
		public static bool Show(AdPlacementType type, string placement, bool autoLoad = true)
			=> management.Show(type, placement, autoLoad);

		/// <inheritdoc cref="AdManagement.Show(Advertising.AdPlacementEntry,bool)"/>
		public static bool Show(AdPlacementEntry entry, bool autoLoad = true)
			=> management.Show(entry, autoLoad);

		#endregion

		#region Show Async

		public static Task<AdShowResult> ShowAsync<T>(string placement, CancellationToken cancellationToken, bool autoLoad = true)
			where T : AdPlacementEntry
			=> management.ShowAsync<T>(placement, autoLoad, cancellationToken);

		public static Task<AdShowResult> ShowAsync(AdPlacementEntry entry, CancellationToken cancellationToken, bool autoLoad = true)
			=> management.ShowAsync(entry, autoLoad, cancellationToken);

		public static Task<AdShowResult> ShowAsync(AdPlacementType type, string placement, CancellationToken cancellationToken,
			bool autoLoad = true)
			=> management.ShowAsync(type, placement, autoLoad, cancellationToken);

		#endregion

		#region Load

		public static bool Load<T>(string placement) where T : AdPlacementEntry
			=> management.Load<T>(placement);

		public static bool Load(AdPlacementEntry entry)
			=> management.Load(entry);

		public static bool Load(AdPlacementType type, string placement)
			=> management.Load(type, placement);

		#endregion

		#region Load Async

		public static Task<bool> LoadAsync<T>(string placement, CancellationToken cancellationToken) where T : AdPlacementEntry
			=> management.LoadAsync<T>(placement, cancellationToken);

		public static Task<bool> Load(AdPlacementEntry entry, CancellationToken cancellationToken)
			=> management.LoadAsync(entry, cancellationToken);

		public static Task<bool> Load(AdPlacementType type, string placement, CancellationToken cancellationToken)
			=> management.LoadAsync(type, placement, cancellationToken);

		#endregion

#if DebugLog
		public static IAdvertisingIntegration Integration => management.Integration;

		/// <returns>Предыдущую интеграцию</returns>
		public static IAdvertisingIntegration SetIntegration(IAdvertisingIntegration integration) =>
			management.SetIntegration(integration);
#endif

		public static void Bind(IAdvertisingService service) => management.Bind(service);
		public static void Unbind() => management.Unbind();
	}

	public enum AdShowResult
	{
		Success,
		Failed,
		Canceled
	}
}
