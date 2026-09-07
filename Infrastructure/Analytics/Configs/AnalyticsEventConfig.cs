using System;
using Content;
using Sapientia;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Analytics
{
	/// <summary>
	/// Имя события в конкретной интеграции
	/// </summary>
	[Serializable]
	public struct AnalyticsEventAlias
	{
		public ContentReference<AnalyticsIntegrationConfig> integration;

		public string alias;
	}

	[Constants]
	[Serializable]
	public struct AnalyticsEventConfig : IValidatable
	{
		[Tooltip("Событие уходит не под своим id, а с динамическим префиксом: <окно>_window_shown. " +
			"Само по себе, без префикса, не отправляется никогда")]
		[InfoBox("Итоговый id собирается в коде как <префикс>_<id>, поэтому алиас на такое событие не сработает — " +
			"переименовывать нужно там, где событие отправляется", InfoMessageType.Warning, nameof(prefixed))]
		public bool prefixed;

		[Tooltip("Интеграции, в которых событие называется иначе. Кому событие уходит — решает маршрут интеграции")]
		public AnalyticsEventAlias[] aliases;

		public bool TryGetAlias(in ContentReference<AnalyticsIntegrationConfig> integration, out string result)
		{
			if (!aliases.IsNullOrEmpty())
			{
				foreach (var candidate in aliases)
				{
					if (candidate.integration != integration)
						continue;

					result = candidate.alias;
					return true;
				}
			}

			result = null;
			return false;
		}

		public bool Validate(out string message)
		{
			if (prefixed && !aliases.IsNullOrEmpty())
			{
				message = "Aliases will never be applied: event id is composed at runtime as <prefix>_<id>";
				return false;
			}

			message = null;
			return true;
		}
	}
}
