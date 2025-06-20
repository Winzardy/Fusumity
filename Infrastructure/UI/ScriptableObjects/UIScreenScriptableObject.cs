using UI.Screens;
using UnityEngine;

namespace Content.ScriptableObjects.UI
{
	[CreateAssetMenu(menuName = ContentUIEditorConstants.CREATE_MENU + "Screen", fileName = "New Screen")]
	public class UIScreenScriptableObject : ContentEntryScriptableObject<UIScreenEntry>
	{
	}
}
