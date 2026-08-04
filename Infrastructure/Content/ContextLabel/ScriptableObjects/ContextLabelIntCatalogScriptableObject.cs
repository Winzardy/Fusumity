using System.Collections.Generic;
using Content.ContextLabel;
using UnityEngine;

namespace Content.ScriptableObjects
{
	[CreateAssetMenu(menuName = ContentMenuConstants.CREATE_MENU + "Misc/Label/Int Catalog", fileName = "ContextLabels_Int_New")]
	public class ContextLabelIntCatalogScriptableObject : ContentEntryScriptableObject<ContextLabelCatalog<int>>
	{
		public override bool UseConstants { get => true; }

		// У int-ключа иерархии нет, имя константы берём из лейбла — единственного
		// человекочитаемого текста, который у записи есть
		public override IEnumerable<ContentConstantEntry> EnumerateConstants()
		{
			foreach (var key in Value.GetKeys())
				yield return new ContentConstantEntry(Value[key], key, summary: Value[key]);
		}
	}
}
