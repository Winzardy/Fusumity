using System;
using System.Collections.Generic;
using UnityEngine;

namespace Analytics
{
	[Serializable]
	public struct AnalyticsSettings
	{
		[SerializeReference]
		public List<IAnalyticsIntegration> integrations;

		[Space]
		public List<string> disableAggregators;
	}
}
