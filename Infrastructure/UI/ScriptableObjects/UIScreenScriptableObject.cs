using UI.Screens;
using UnityEngine;

namespace Content.ScriptableObjects.UI
{
	[CreateAssetMenu(menuName = ContentUIEditorConstants.CREATE_MENU + "Screen", fileName = "0_Screen_New")]
	public class UIScreenScriptableObject : ContentEntryScriptableObject<UIScreenEntry>
	{
	}
}
