using System;
using Content;
using Fusumity.Attributes;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Analytics
{
	public enum AnalyticsRouteMode
	{
		/// <summary>
		/// Отправлять все события, кроме перечисленных
		/// </summary>
		All = 0,

		/// <summary>
		/// Не отправлять ничего, кроме перечисленных
		/// </summary>
		None = 1
	}

	/// <summary>
	/// Правило, по которому события доезжают до интеграции
	/// </summary>
	[Serializable]
	public struct AnalyticsRoute
	{
		public AnalyticsRouteMode mode;

		[Tooltip("Исключения из режима: при All — что не отправлять, при None — что всё-таки отправлять")]
		public ContentReference<AnalyticsEventConfig>[] exceptions;

		public bool Contains(in ContentReference<AnalyticsEventConfig> reference)
		{
			if (exceptions.IsNullOrEmpty())
				return false;

			foreach (var exception in exceptions)
			{
				if (exception == reference)
					return true;
			}

			return false;
		}
	}

	[Constants]
	[Serializable]
	public struct AnalyticsIntegrationConfig
	{
		[SerializeReference]
		[DarkCardBox(Indent = false)]
		public IAnalyticsIntegration integration;

		[HideLabel]
		[TitleGroup("Route", "Какие события доезжают до интеграции")]
		public AnalyticsRoute route;
	}
}
