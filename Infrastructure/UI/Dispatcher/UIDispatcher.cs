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

		public static T Get<T>()
			where T : IUIDispatcher
			=> management.Get<T>();

		public static void Get<T>(out T dispatcher)
			where T : IUIDispatcher
		{
			dispatcher = Get<T>();
		}

		/// <summary>
		/// Возвращает слой по айди (если его нет создаст)
		/// </summary>
		public static UILayerLayout Get(string id) => management[id];

		/// <summary>
		/// Возвращает слой по айди (без создания)
		/// </summary>
		public static bool TryGet(string id, out UILayerLayout layer) => management.TryGet(id, out layer);
	}
}
