using System.Collections.Generic;
using Content.ContextLabel;
using UnityEngine;

namespace Content.ScriptableObjects
{
	[CreateAssetMenu(menuName = ContentMenuConstants.CREATE_MENU + "Misc/Label/String Catalog", fileName = "ContextLabels_String_New")]
	public class ContextLabelStringCatalogScriptableObject : ContentEntryScriptableObject<ContextLabelCatalog<string>>
	{
		public override bool UseConstants { get => true; }

		// Ключ несёт иерархию ("hair/00"): из него и имя константы, и группа-заголовок
		public override IEnumerable<ContentConstantEntry> EnumerateConstants()
		{
			foreach (var key in Value.GetKeys())
			{
				var separatorIndex = key.LastIndexOf('/');
				var group = separatorIndex > 0 ? key[..separatorIndex] : null;
				yield return new ContentConstantEntry(key, key, group, Value[key]);
			}
		}
	}
}
