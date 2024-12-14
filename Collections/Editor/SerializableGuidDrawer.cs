using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Collections.Editor
{
	public class SerializableGuidDrawer : OdinValueDrawer<SerializableGuid>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var value = ValueEntry.SmartValue;
			var guid = value.guid.ToString();
			var newGuid =  new Guid(EditorGUILayout.TextField(label, guid));

			if (value.guid != newGuid)
			{
				value.guid = newGuid;
				ValueEntry.SmartValue = value;
			}
		}
	}
}
