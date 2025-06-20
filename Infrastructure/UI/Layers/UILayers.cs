using System.Runtime.CompilerServices;
using Sapientia;

namespace UI.Layers
{
	public class UILayers : StaticProvider<UILayersManagement>
	{
		private static UILayersManagement management
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
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
