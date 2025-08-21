using UI.Tabs;
using UnityEngine;

namespace Content.ScriptableObjects.UI
{
	[CreateAssetMenu(menuName = ContentUIEditorConstants.CREATE_MENU + "Tab", fileName = "0_Tab_New")]
	public class UITabScriptableObject : ContentEntryScriptableObject<UITabEntry>
	{
	}
}
