using Content;
using Sapientia.Extensions;

namespace Analytics
{
	/// <summary>
	/// Решает, доедет ли событие до интеграции и под каким именем
	/// </summary>
	/// <remarks>
	/// События рассылаются веером во все интеграции, но не каждой нужны все:
	/// у MMP короткий список под фиксированными именами, по которым в кабинете настраиваются цели
	/// </remarks>
	public static class AnalyticsEventRouter
	{
		public static bool TryRoute(in ContentReference<AnalyticsIntegrationConfig> integration,
			in AnalyticsEventPayload source, out AnalyticsEventPayload result)
		{
			result = source;

			if (!integration.IsValid())
				return true;

			// Событию не обязательно иметь конфиг: без него работает только режим маршрута
			var hasConfig = !source.id.IsNullOrEmpty() && ContentManager.Contains<AnalyticsEventConfig>(source.id);
			var reference = hasConfig ? source.id.ToReference<AnalyticsEventConfig>() : default;

			ref readonly var config = ref integration.Read();

			var listed = hasConfig && config.route.Contains(in reference);

			if (config.route.mode == AnalyticsRouteMode.All ? listed : !listed)
				return false;

			if (hasConfig && reference.Read().TryGetAlias(in integration, out var alias) && !alias.IsNullOrEmpty())
				result.id = alias;

			return true;
		}
	}
}
