using Fusumity.Collections;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	public class SerializableDictionaryDrawer : OdinValueDrawer<ISerializableDictionary>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var dictionary = ValueEntry.SmartValue;
			var sync = dictionary.Length == dictionary.Count;

			if (!sync)
			{
				var msg = $"Out of sync: serialized length ({dictionary.Length}) ≠ runtime length ({dictionary.Count})\n" +
					"Synchronization is required!";
				var buttonLabel = "Sync";

				if (FusumityEditorGUILayout.MessageBoxButton(msg, buttonLabel, MessageType.Error))
				{
					dictionary.Sync();

					Property.MarkSerializationRootDirty();
					ValueEntry.ApplyChanges();
				}
			}

			CallNextDrawer(label);
		}
	}
}
