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
			var needSync = dictionary.NeedSync();

			if (needSync)
			{
				var msg =
					"Dictionary is out of sync\n" +
					"Serialized data does not match runtime data\n\n" +
					"Synchronization is required!";
				var buttonLabel = "Sync";

				Debug.LogError(msg);

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
