using System.Collections.Generic;
using Content.Keys;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Content.ScriptableObjects
{
	[CreateAssetMenu(menuName = ContentMenuConstants.CREATE_MENU + "Misc/Key Catalog/String", fileName = "KeyCatalog_String_New")]
	[MovedFrom(true, sourceClassName: "ContextLabelStringCatalogScriptableObject")]
	public class StringKeyCatalogScriptableObject : ContentEntryScriptableObject<KeyCatalog<string>>
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
