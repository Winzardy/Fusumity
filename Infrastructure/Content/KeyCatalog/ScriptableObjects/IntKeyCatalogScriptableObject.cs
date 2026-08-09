using System.Collections.Generic;
using Content.Keys;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Content.ScriptableObjects
{
	[CreateAssetMenu(menuName = ContentMenuConstants.CREATE_MENU + "Misc/Key Catalog/Int", fileName = "KeyCatalog_Int_New")]
	[MovedFrom(true, sourceClassName: "ContextLabelIntCatalogScriptableObject")]
	public class IntKeyCatalogScriptableObject : ContentEntryScriptableObject<KeyCatalog<int>>
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
