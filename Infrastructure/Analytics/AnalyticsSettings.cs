using System;
using System.Collections.Generic;
using Content;
using Sapientia;
using Sapientia.Collections;
using UnityEngine;

namespace Analytics
{
	[Serializable]
	public struct AnalyticsSettings : IValidatable
	{
		public ContentReference<AnalyticsIntegrationConfig>[] integrations;

		[Space]
		public List<string> disableAggregators;

		/// <remarks>
		/// Две одинаковые реализации отправят каждое событие дважды, а маршруты у них при этом будут разные —
		/// поймать такое по данным потом почти невозможно
		/// </remarks>
		public bool Validate(out string message)
		{
			if (!integrations.IsNullOrEmpty())
			{
				for (var i = 0; i < integrations.Length; i++)
				{
					if (!TryGetIntegrationType(in integrations[i], out var type))
						continue;

					for (var j = i + 1; j < integrations.Length; j++)
					{
						if (!TryGetIntegrationType(in integrations[j], out var other) || other != type)
							continue;

						message = $"Duplicate integration [ {type.Name} ] " +
							$"in configs [ {integrations[i].ToId()} ] and [ {integrations[j].ToId()} ]";
						return false;
					}
				}
			}

			message = null;
			return true;
		}

		private static bool TryGetIntegrationType(in ContentReference<AnalyticsIntegrationConfig> reference, out Type result)
		{
			result = null;

			// Незагруженный или пустой конфиг проверять нечем, его поймает своя валидация
			if (!reference.IsValid())
				return false;

			var integration = reference.Read().integration;

			if (integration == null)
				return false;

			result = integration.GetType();
			return true;
		}
	}
}
