using UI.Windows;
using UnityEngine;

namespace Content.ScriptableObjects.UI
{
	[CreateAssetMenu(menuName = ContentUIEditorConstants.CREATE_MENU + "Window", fileName = "New Window")]
	public class UIWindowScriptableObject : ContentEntryScriptableObject<UIWindowEntry>
	{
	}
}
