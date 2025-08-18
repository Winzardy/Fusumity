using UI.Windows;
using UnityEngine;

namespace Content.ScriptableObjects.UI
{
	[CreateAssetMenu(menuName = ContentUIEditorConstants.CREATE_MENU + "Window", fileName = "0_Window_New")]
	public class UIWindowScriptableObject : ContentEntryScriptableObject<UIWindowEntry>
	{
	}
}
