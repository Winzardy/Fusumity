using Advertising.UnityLevelPlay;
using UnityEngine;

namespace Content.ScriptableObjects.Advertising
{
	[CreateAssetMenu(menuName = ContentAdvertisingEditorConstants.CREATE_MENU + "Settings/UnityLevelPlay",
		fileName = "Ad_Settings_UnityLevelPlay")]
	public class UnityLevelPlaySettingsScriptableObject : SingleContentEntryScriptableObject<UnityLevelPlaySettings>
	{
	}
}
