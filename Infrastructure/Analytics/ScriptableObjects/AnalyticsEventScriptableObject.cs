using Analytics;
using UnityEngine;

namespace Content.ScriptableObjects.Analytics
{
	[CreateAssetMenu(menuName = ContentMenuConstants.CREATE_MENU + "Analytics/Event Config", fileName = "Analytics_Event_New",
		order = ContentMenuConstants.ENTRY_PRIORITY)]
	public class AnalyticsEventScriptableObject : ContentEntryScriptableObject<AnalyticsEventConfig>
	{
	}
}
