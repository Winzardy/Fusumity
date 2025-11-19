using UI.Layers;
using UnityEngine;

namespace Content.ScriptableObjects.UI
{
	[CreateAssetMenu(menuName = ContentUIEditorConstants.CREATE_MENU + "Layer", fileName = "0_Layer_New", order = 99)]
	public class UILayerScriptableObject : ContentEntryScriptableObject<UILayerConfig>
	{
	}
}
