using Content.ContextLabel;
using UnityEngine;

namespace Content.ScriptableObjects.Labeling
{
	// [Constants]
	// TODO: сделать генерацию констант через SO
	// Хороший вопрос что тип данных может быть помечен Constants, а SO's можеть быть несколько... Не подумла об этом
	[CreateAssetMenu(menuName = ContentMenuConstants.CREATE_MENU + "Misc/Label/Int Catalog", fileName = "Labels_Int_New")]
	public class ContextLabelIntCatalogScriptableObject : ContentEntryScriptableObject<ContextLabelCatalog<int>>
	{
	}
}
