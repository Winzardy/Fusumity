using System.Runtime.CompilerServices;
using Sapientia;

namespace UI
{
	public partial class UIDispatcher : StaticProvider<UIManagement>
	{
		private static UIManagement management
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		public static bool IsInitialized => management != null;

		public static T Get<T>()
			where T : class, IWidgetDispatcher
			=> management.Get<T>();

		public static void Get<T>(out T dispatcher)
			where T : class, IWidgetDispatcher
			=> dispatcher = Get<T>();

		public static bool TryGet<T>(out T dispatcher)
			where T : class, IWidgetDispatcher
		{
			dispatcher = null;
			if (!IsInitialized)
				return false;
			dispatcher = Get<T>();
			return dispatcher != null;
		}

		/// <summary>
		/// Возвращает слой по айди (если его нет создаст)
		/// </summary>
		public static UILayerLayout GetLayer(string id) => management[id];

		/// <summary>
		/// Возвращает слой по айди (без создания)
		/// </summary>
		public static bool TryGetLayer(string id, out UILayerLayout layer) => management.TryGet(id, out layer);
	}
}
