using Analytics;
using UnityEngine;

namespace Content.ScriptableObjects.Analytics
{
	[CreateAssetMenu(menuName = ContentMenuConstants.CREATE_MENU + "Analytics/Integration Config",
		fileName = "Analytics_Integration_New", order = ContentMenuConstants.ENTRY_PRIORITY)]
	public class AnalyticsIntegrationScriptableObject : ContentEntryScriptableObject<AnalyticsIntegrationConfig>
	{
	}
}
