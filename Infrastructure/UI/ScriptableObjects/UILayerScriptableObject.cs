using UI.Layers;
using UnityEngine;

namespace Content.ScriptableObjects.UI
{
	[CreateAssetMenu(menuName = ContentUIEditorConstants.CREATE_MENU + "Layer", fileName = "New Layer", order = 99)]
	public class UILayerScriptableObject : ContentEntryScriptableObject<UILayerEntry>
	{
	}
}
