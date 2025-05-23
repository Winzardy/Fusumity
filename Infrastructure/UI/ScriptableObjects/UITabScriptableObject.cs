using UI.Tabs;
using UnityEngine;

namespace Content.ScriptableObjects.UI
{
	[CreateAssetMenu(menuName = ContentUIEditorConstants.CREATE_MENU + "Tab", fileName = "New Tab")]
	public class UITabScriptableObject : ContentEntryScriptableObject<UITabEntry>
	{
	}
}
