using Content.ContextLabel;
using UnityEngine;

namespace Content.ScriptableObjects.Labeling
{
	// [Constants]
	// TODO: сделать генерацию констант через SO
	// Хороший вопрос что тип данных может быть помечен Constants, а SO's можеть быть несколько... Не подумла об этом
	[CreateAssetMenu(menuName = ContentMenuConstants.CREATE_MENU + "Misc/Label/String Catalog", fileName = "ContextLabels_String_New")]
	public class ContextLabelStringCatalogScriptableObject : ContentEntryScriptableObject<ContextLabelCatalog<string>>
	{
	}
}
