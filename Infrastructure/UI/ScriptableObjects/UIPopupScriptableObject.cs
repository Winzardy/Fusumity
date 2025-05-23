using UI.Popups;
using UnityEngine;

namespace Content.ScriptableObjects.UI
{
	[CreateAssetMenu(menuName = ContentUIEditorConstants.CREATE_MENU + "Popup", fileName = "New Popup")]
	public class UIPopupScriptableObject : ContentEntryScriptableObject<UIPopupEntry>
	{
	}
}
